using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Auth;

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
        });

        group.MapPost("/login", async (LoginRequest req, HttpContext ctx,
            ICommandHandler<LoginCommand, AuthResponse> handler, IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            var auth = await handler.Handle(new LoginCommand(req), ct);
            await IssueRefreshCookie(ctx, refresh, auth.UserId, ct);
            return Results.Ok(auth);
        });

        // Обновление access-токена по refresh-cookie: ротация + новый cookie.
        group.MapPost("/refresh", async (HttpContext ctx, IRefreshTokenService refresh, CancellationToken ct) =>
        {
            var raw = RefreshTokenCookie.Read(ctx);
            if (string.IsNullOrEmpty(raw)) return Results.Unauthorized();
            var rot = await refresh.RotateAsync(raw, RefreshTokenCookie.Meta(ctx), ct);
            RefreshTokenCookie.Set(ctx, rot.RawRefreshToken, rot.RefreshExpiresAt);
            return Results.Ok(new AuthResponse(rot.AccessToken, rot.UserId, rot.Email, rot.DisplayName));
        });

        // Выход: отзыв семейства текущего refresh-токена и очистка cookie.
        group.MapPost("/logout", async (HttpContext ctx, IRefreshTokenService refresh, CancellationToken ct) =>
        {
            var raw = RefreshTokenCookie.Read(ctx);
            if (!string.IsNullOrEmpty(raw)) await refresh.RevokeFamilyAsync(raw, ct);
            RefreshTokenCookie.Clear(ctx);
            return Results.NoContent();
        });
    }

    private static async Task IssueRefreshCookie(
        HttpContext ctx, IRefreshTokenService refresh, Guid userId, CancellationToken ct)
    {
        var (raw, expires) = await refresh.IssueAsync(userId, RefreshTokenCookie.Meta(ctx), ct);
        RefreshTokenCookie.Set(ctx, raw, expires);
    }
}
