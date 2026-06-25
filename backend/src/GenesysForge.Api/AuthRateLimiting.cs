using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GenesysForge.Api;

public static class AuthRateLimiting
{
    public const string SensitivePolicy = "auth-sensitive";
    public const string SessionPolicy = "auth-session";
    public const string PublicPolicy = "auth-public";

    public static IServiceCollection AddAuthRateLimiting(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Слишком много запросов. Повторите позже." },
                    cancellationToken);
            };

            options.AddPolicy(SensitivePolicy, context =>
                FixedWindow(context, config, "RateLimiting:AuthSensitive", 10, 60));
            options.AddPolicy(SessionPolicy, context =>
                FixedWindow(context, config, "RateLimiting:AuthSession", 30, 60));
            options.AddPolicy(PublicPolicy, context =>
                FixedWindow(context, config, "RateLimiting:AuthPublic", 60, 60));
        });
        return services;
    }

    private static RateLimitPartition<string> FixedWindow(
        HttpContext context,
        IConfiguration config,
        string section,
        int defaultPermitLimit,
        int defaultWindowSeconds)
    {
        var permitLimit = Positive(config[$"{section}:PermitLimit"], defaultPermitLimit);
        var windowSeconds = Positive(config[$"{section}:WindowSeconds"], defaultWindowSeconds);
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    private static int Positive(string? value, int fallback) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
}
