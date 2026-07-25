using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Channels;

namespace GenesysForge.Api.Analytics;

public sealed record AriadneServerEvent(
    Guid EventId,
    string Name,
    string UserId,
    Guid? AnonymousId,
    IReadOnlyDictionary<string, object?>? Properties = null);

/// <summary>
/// Ограниченная фоновая очередь доставки серверных событий в «Ариадну».
/// Отказ аналитики изолирован от бизнес-операции: очередь роняет самые старые события,
/// а вызывающая сторона никогда не ждёт сеть и не получает исключений.
/// </summary>
public sealed class AriadneAnalytics(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<AriadneAnalytics> logger) : BackgroundService
{
    private readonly Channel<AriadneServerEvent> _queue =
        Channel.CreateBounded<AriadneServerEvent>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    private string? Endpoint => configuration["ARIADNE_ENDPOINT"]?.TrimEnd('/');
    private string? ServerKey => configuration["ARIADNE_SERVER_KEY"];

    public bool Enabled => Uri.TryCreate(Endpoint, UriKind.Absolute, out _) &&
                           ServerKey?.StartsWith("srv_", StringComparison.Ordinal) == true;

    /// <summary>Подтверждённая регистрация. <paramref name="anonymousId"/> связывает её с браузерным визитом.</summary>
    public void TrackRegistrationCompleted(Guid userId, Guid? anonymousId, string registrationType)
    {
        if (!Enabled) return;
        _queue.Writer.TryWrite(new(Guid.NewGuid(), "registration_completed", userId.ToString(), anonymousId,
            new Dictionary<string, object?> { ["registration_type"] = registrationType }));
    }

    /// <summary>Активация: пользователь дошёл до целевого действия продукта.</summary>
    public void TrackUserActivated(Guid userId, Guid? anonymousId, string activationType)
    {
        if (!Enabled) return;
        _queue.Writer.TryWrite(new(Guid.NewGuid(), "user_activated", userId.ToString(), anonymousId,
            new Dictionary<string, object?> { ["activation_type"] = activationType }));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!Enabled) continue;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(2));
                    var client = clients.CreateClient();
                    using var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/api/v1/server-events");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ServerKey);
                    request.Content = JsonContent.Create(new
                    {
                        events = new[]
                        {
                            new
                            {
                                id = item.EventId,
                                name = item.Name,
                                timestamp = DateTimeOffset.UtcNow,
                                anonymousId = item.AnonymousId,
                                userId = item.UserId,
                                properties = item.Properties,
                            },
                        },
                    });
                    using var response = await client.SendAsync(request, timeout.Token);
                    if (response.IsSuccessStatusCode) break;
                    if (response.StatusCode != HttpStatusCode.TooManyRequests &&
                        (int)response.StatusCode < 500) break;
                    if (attempt == 2)
                        logger.LogWarning("Ariadne delivery failed with status {StatusCode}", (int)response.StatusCode);
                }
                catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
                {
                    if (attempt == 2) logger.LogWarning("Ariadne delivery failed after retries");
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) * (0.75 + Random.Shared.NextDouble() * 0.5)),
                    stoppingToken);
            }
        }
    }
}

public static class AriadneHttpContextExtensions
{
    /// <summary>
    /// Анонимный ID браузерного tracker'а; без него серверное событие не склеится с визитом.
    /// Контекст допускается null — контроллер, созданный в тесте напрямую, его не имеет.
    /// </summary>
    public static Guid? AriadneAnonymousId(this HttpContext? context) =>
        context is not null && Guid.TryParse(context.Request.Headers["X-Ariadne-Anonymous-Id"], out var anonymousId)
            ? anonymousId
            : null;
}
