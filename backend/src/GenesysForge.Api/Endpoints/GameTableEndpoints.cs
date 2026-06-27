using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.GameTable;

namespace GenesysForge.Api.Endpoints;

public static class GameTableEndpoints
{
    public static void MapGameTable(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/campaigns/{campaignId:guid}/session").RequireAuthorization();

        // После любой успешной мутации сцены уведомляем подписчиков кампании (realtime).
        group.AddEndpointFilter(async (ctx, next) =>
        {
            var result = await next(ctx);
            if (ctx.HttpContext.Request.Method != HttpMethods.Get &&
                ctx.HttpContext.Request.RouteValues["campaignId"] is string cid &&
                Guid.TryParse(cid, out var campaignId))
            {
                await ctx.HttpContext.RequestServices
                    .GetRequiredService<ICampaignNotifier>().GameTableChangedAsync(campaignId);
            }
            return result;
        });

        group.MapGet("/", async (Guid campaignId, ClaimsPrincipal user,
            IQueryHandler<GetSessionQuery, GameSessionDto?> handler, CancellationToken ct) =>
        {
            // Нет активной сцены → 204 (пустое тело сломало бы десериализацию у клиента).
            var session = await handler.Handle(new GetSessionQuery(user.UserId(), campaignId), ct);
            return session is null ? Results.NoContent() : Results.Ok(session);
        });

        group.MapPost("/", async (Guid campaignId, CreateSessionRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateSessionCommand, GameSessionDto> handler, CancellationToken ct) =>
        {
            var session = await handler.Handle(new CreateSessionCommand(user.UserId(), campaignId, req), ct);
            return Results.Created($"/api/campaigns/{campaignId}/session", session);
        });

        group.MapPatch("/", async (Guid campaignId, UpdateSessionRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateSessionCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateSessionCommand(user.UserId(), campaignId, req), ct)));

        group.MapPost("/reset", async (Guid campaignId, ClaimsPrincipal user,
                ICommandHandler<ResetSessionCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ResetSessionCommand(user.UserId(), campaignId), ct)));

        group.MapPost("/next-turn", async (Guid campaignId, ClaimsPrincipal user,
                ICommandHandler<NextTurnCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new NextTurnCommand(user.UserId(), campaignId), ct)));

        group.MapDelete("/", async (Guid campaignId, ClaimsPrincipal user,
            ICommandHandler<EndSessionCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new EndSessionCommand(user.UserId(), campaignId), ct);
            return Results.NoContent();
        });

        // Участники
        group.MapPost("/participants", async (Guid campaignId, AddParticipantRequest req, ClaimsPrincipal user,
                ICommandHandler<AddParticipantCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new AddParticipantCommand(user.UserId(), campaignId, req), ct)));

        group.MapPatch("/participants/{participantId:guid}", async (Guid campaignId, Guid participantId,
                UpdateParticipantRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateParticipantCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new UpdateParticipantCommand(user.UserId(), campaignId, participantId, req), ct)));

        group.MapPost("/participants/{participantId:guid}/activate", async (Guid campaignId, Guid participantId,
                ActivateAbilityRequest req, ClaimsPrincipal user,
                ICommandHandler<ActivateAbilityCommand, ActivateAbilityResult> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new ActivateAbilityCommand(user.UserId(), campaignId, participantId, req), ct)));

        group.MapDelete("/participants/{participantId:guid}", async (Guid campaignId, Guid participantId,
            ClaimsPrincipal user, ICommandHandler<RemoveParticipantCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveParticipantCommand(user.UserId(), campaignId, participantId), ct);
            return Results.NoContent();
        });

        // Слоты инициативы
        group.MapPost("/slots", async (Guid campaignId, AddSlotRequest req, ClaimsPrincipal user,
                ICommandHandler<AddSlotCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new AddSlotCommand(user.UserId(), campaignId, req), ct)));

        group.MapPatch("/slots/{slotId:guid}", async (Guid campaignId, Guid slotId, UpdateSlotRequest req,
                ClaimsPrincipal user, ICommandHandler<UpdateSlotCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateSlotCommand(user.UserId(), campaignId, slotId, req), ct)));

        group.MapDelete("/slots/{slotId:guid}", async (Guid campaignId, Guid slotId, ClaimsPrincipal user,
            ICommandHandler<RemoveSlotCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveSlotCommand(user.UserId(), campaignId, slotId), ct);
            return Results.NoContent();
        });
    }
}
