using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Auth;
using GenesysForge.Api;
using Microsoft.Extensions.Configuration;

namespace GenesysForge.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, HttpContext ctx,
            ICommandHandler<RegisterUserCommand, AuthResponse> handler, IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            var auth = await handler.Handle(new RegisterUserCommand(req), ct);
            await IssueRefreshCookie(ctx, refresh, auth.UserId, ct);
            return Results.Ok(auth);
        }).RequireRateLimiting(AuthRateLimiting.SensitivePolicy);

        group.MapPost("/login", async (LoginRequest req, HttpContext ctx,
            ICommandHandler<LoginCommand, AuthResponse> handler, IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            var auth = await handler.Handle(new LoginCommand(req), ct);
            await IssueRefreshCookie(ctx, refresh, auth.UserId, ct);
            return Results.Ok(auth);
        }).RequireRateLimiting(AuthRateLimiting.SensitivePolicy);

        // Сброс пароля. Запрос всегда отвечает 204 (не раскрывает наличие аккаунта);
        // подтверждение по одноразовому токену из письма.
        group.MapPost("/password-reset/request", async (PasswordResetRequestRequest req,
            ICommandHandler<RequestPasswordResetCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RequestPasswordResetCommand(req), ct);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.SensitivePolicy);

        group.MapPost("/password-reset/confirm", async (PasswordResetConfirmRequest req,
            ICommandHandler<ConfirmPasswordResetCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ConfirmPasswordResetCommand(req), ct);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.SensitivePolicy);

        // Вход через Google: фронтенд присылает ID-токен от Google Identity Services.
        group.MapPost("/google", async (GoogleSignInRequest req, HttpContext ctx,
            ICommandHandler<GoogleSignInCommand, AuthResponse> handler, IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            var auth = await handler.Handle(new GoogleSignInCommand(req), ct);
            await IssueRefreshCookie(ctx, refresh, auth.UserId, ct);
            return Results.Ok(auth);
        }).RequireRateLimiting(AuthRateLimiting.SensitivePolicy);

        // Какие внешние провайдеры входа доступны (для отрисовки кнопок). Публично.
        group.MapGet("/providers", (IConfiguration config) =>
            Results.Ok(new AuthProvidersResponse(config["Auth:Google:ClientId"])))
            .RequireRateLimiting(AuthRateLimiting.PublicPolicy);

        // Обновление access-токена по refresh-cookie: ротация + новый cookie.
        group.MapPost("/refresh", async (HttpContext ctx, IRefreshTokenService refresh, CancellationToken ct) =>
        {
            var raw = RefreshTokenCookie.Read(ctx);
            if (string.IsNullOrEmpty(raw)) return Results.Unauthorized();
            var rot = await refresh.RotateAsync(raw, RefreshTokenCookie.Meta(ctx), ct);
            RefreshTokenCookie.Set(ctx, rot.RawRefreshToken, rot.RefreshExpiresAt);
            return Results.Ok(new AuthResponse(rot.AccessToken, rot.UserId, rot.Email, rot.DisplayName));
        }).RequireRateLimiting(AuthRateLimiting.SessionPolicy);

        // Выход: отзыв семейства текущего refresh-токена и очистка cookie.
        group.MapPost("/logout", async (HttpContext ctx, IRefreshTokenService refresh, CancellationToken ct) =>
        {
            var raw = RefreshTokenCookie.Read(ctx);
            if (!string.IsNullOrEmpty(raw)) await refresh.RevokeFamilyAsync(raw, ct);
            RefreshTokenCookie.Clear(ctx);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.SessionPolicy);
    }

    private static async Task IssueRefreshCookie(
        HttpContext ctx, IRefreshTokenService refresh, Guid userId, CancellationToken ct)
    {
        var (raw, expires) = await refresh.IssueAsync(userId, RefreshTokenCookie.Meta(ctx), ct);
        RefreshTokenCookie.Set(ctx, raw, expires);
    }
}
