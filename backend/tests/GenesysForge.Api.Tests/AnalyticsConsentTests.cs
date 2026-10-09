using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GenesysForge.Application.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenesysForge.Api.Tests;

public class AnalyticsConsentTests
{
    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/v1/auth/register")]
    public async Task Registration_OnlySendsAnalyticsForConsentingBrowser(string path)
    {
        var anonymousId = Guid.NewGuid();
        using var delivery = new CaptureDelivery(anonymousId);
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ARIADNE_ENDPOINT"] = "https://analytics.test",
                    ["ARIADNE_SERVER_KEY"] = "srv_test",
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(delivery);
            });
        });
        using var client = factory.CreateClient();
        // Ожидая последний event в FIFO-канале, проверяем отсутствие событий
        // от предыдущих регистраций без ID согласившегося браузера.
        foreach (var header in new[] { null, "invalid-id", anonymousId.ToString() })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new RegisterRequest($"{Guid.NewGuid():N}@test.local", "password123", "Tester")),
            };
            if (header is not null) request.Headers.Add("X-Ariadne-Anonymous-Id", header);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
        }
        await delivery.ConsentedEvent.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var payload = Assert.Single(delivery.Payloads);
        using var json = JsonDocument.Parse(payload);
        var item = json.RootElement.GetProperty("events")[0];
        Assert.Equal("registration_completed", item.GetProperty("name").GetString());
        Assert.Equal(anonymousId, item.GetProperty("anonymousId").GetGuid());
    }

    private sealed class CaptureDelivery(Guid consentedId) : HttpMessageHandler, IHttpClientFactory
    {
        public ConcurrentQueue<string> Payloads { get; } = new();
        public TaskCompletionSource ConsentedEvent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var payload = await request.Content!.ReadAsStringAsync(ct);
            Payloads.Enqueue(payload);
            using var json = JsonDocument.Parse(payload);
            if (json.RootElement.GetProperty("events")[0].GetProperty("anonymousId").ToString() == consentedId.ToString())
                ConsentedEvent.TrySetResult();
            return new(HttpStatusCode.Accepted);
        }
    }
}
