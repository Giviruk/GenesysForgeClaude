using System.Security.Claims;
using GenesysForge.Api;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Account;

namespace GenesysForge.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccount(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user,
                IQueryHandler<GetAccountQuery, AccountDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetAccountQuery(user.UserId()), ct)));

        group.MapPatch("/", async (UpdateAccountRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateAccountCommand, AccountDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateAccountCommand(user.UserId(), req), ct)));

        // Аватар загружается сырым телом запроса; формат и размер проверяются по содержимому.
        // Под rate limit, чтобы загрузками нельзя было забить хранилище.
        group.MapPost("/avatar", async (HttpRequest request, ClaimsPrincipal user,
            ICommandHandler<UploadAvatarCommand, AccountDto> handler, CancellationToken ct) =>
        {
            var content = await UploadBody.ReadImageAsync(request, ct);
            return Results.Ok(await handler.Handle(new UploadAvatarCommand(user.UserId(), content), ct));
        }).RequireRateLimiting(AuthRateLimiting.SessionPolicy);

        // Смена пароля отзывает все сессии; текущему устройству выдаём свежий refresh-cookie,
        // чтобы пользователь остался в сессии. Чувствительная операция — под rate limit.
        group.MapPost("/change-password", async (ChangePasswordRequest req, HttpContext ctx, ClaimsPrincipal user,
            ICommandHandler<ChangePasswordCommand, Unit> handler, IRefreshTokenService refresh,
            CancellationToken ct) =>
        {
            await handler.Handle(new ChangePasswordCommand(user.UserId(), req), ct);
            var (raw, expires) = await refresh.IssueAsync(user.UserId(), RefreshTokenCookie.Meta(ctx), ct);
            RefreshTokenCookie.Set(ctx, raw, expires);
            return Results.NoContent();
        }).RequireRateLimiting(AuthRateLimiting.SensitivePolicy);
    }
}
