using GenesysForge.Application.Abstractions;

namespace GenesysForge.Api.Endpoints;

/// <summary>
/// Транспорт refresh-токена через HttpOnly-cookie. Путь ограничен /api/auth, поэтому токен
/// уходит только на эндпоинты обновления/выхода, а не на каждый запрос.
/// В Production Secure обязателен, включая HTTP-hop от reverse proxy к API.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "gf_refresh";
    private const string CookiePath = "/api/auth";

    private static CookieOptions Options(HttpContext ctx, DateTimeOffset expires)
    {
        var environment = ctx.RequestServices.GetRequiredService<IHostEnvironment>();
        return new()
        {
            HttpOnly = true,
            Secure = environment.IsProduction() || ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = CookiePath,
            Expires = expires,
        };
    }

    public static void Set(HttpContext ctx, string rawToken, DateTime expiresUtc) =>
        ctx.Response.Cookies.Append(Name, rawToken, Options(ctx, new DateTimeOffset(expiresUtc, TimeSpan.Zero)));

    public static string? Read(HttpContext ctx) => ctx.Request.Cookies[Name];

    public static void Clear(HttpContext ctx) =>
        ctx.Response.Cookies.Append(Name, "", Options(ctx, DateTimeOffset.UnixEpoch));

    public static RequestMeta Meta(HttpContext ctx) => new(
        ctx.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
        ctx.Connection.RemoteIpAddress?.ToString());
}
