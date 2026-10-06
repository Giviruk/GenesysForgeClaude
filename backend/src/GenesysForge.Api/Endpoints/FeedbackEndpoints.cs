using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Feedback;

namespace GenesysForge.Api.Endpoints;

public static class FeedbackEndpoints
{
    /// <summary>
    /// Форма обратной связи доступна и без входа. Вошедший пользователь подставляется по токену,
    /// чтобы в письме был адрес его аккаунта. Лимит частоты — общий с чувствительными auth-запросами.
    /// </summary>
    public static void MapFeedback(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/feedback", async (SendFeedbackRequest req, ClaimsPrincipal user,
            ICommandHandler<SendFeedbackCommand, Unit> handler, CancellationToken ct) =>
        {
            Guid? userId = user.Identity?.IsAuthenticated == true ? user.UserId() : null;
            await handler.Handle(new SendFeedbackCommand(userId, req), ct);
            return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting(AuthRateLimiting.SensitivePolicy);
}
