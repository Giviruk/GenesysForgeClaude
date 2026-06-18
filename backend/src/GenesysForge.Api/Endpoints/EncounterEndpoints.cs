using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Encounters;

namespace GenesysForge.Api.Endpoints;

public static class EncounterEndpoints
{
    public static void MapEncounters(this IEndpointRouteBuilder app)
    {
        // Список/создание привязаны к кампании; работа с конкретным энкаунтером — по его id.
        var campaign = app.MapGroup("/api/campaigns/{campaignId:guid}/encounters").RequireAuthorization();

        campaign.MapGet("/", async (Guid campaignId, ClaimsPrincipal user,
            IQueryHandler<GetEncountersQuery, List<EncounterListItemDto>> handler, CancellationToken ct,
            string? search, string? type, string? tag) =>
            Results.Ok(await handler.Handle(
                new GetEncountersQuery(user.UserId(), campaignId, search, type, tag), ct)));

        campaign.MapPost("/", async (Guid campaignId, EncounterInput input, ClaimsPrincipal user,
            ICommandHandler<CreateEncounterCommand, EncounterDetailDto> handler, CancellationToken ct) =>
        {
            var e = await handler.Handle(new CreateEncounterCommand(user.UserId(), campaignId, input), ct);
            return Results.Created($"/api/encounters/{e.Id}", e);
        });

        var group = app.MapGroup("/api/encounters").RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetEncounterQuery, EncounterDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetEncounterQuery(user.UserId(), id), ct)));

        group.MapPut("/{id:guid}", async (Guid id, EncounterInput input, ClaimsPrincipal user,
                ICommandHandler<UpdateEncounterCommand, EncounterDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateEncounterCommand(user.UserId(), id, input), ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteEncounterCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteEncounterCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        // Участники энкаунтера
        group.MapPost("/{id:guid}/participants", async (Guid id, AddEncounterParticipantRequest req,
                ClaimsPrincipal user, ICommandHandler<AddEncounterParticipantCommand, EncounterDetailDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new AddEncounterParticipantCommand(user.UserId(), id, req), ct)));

        group.MapPost("/{id:guid}/participants/characters", async (Guid id, AddCampaignCharactersRequest req,
                ClaimsPrincipal user, ICommandHandler<AddCampaignCharactersCommand, EncounterDetailDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new AddCampaignCharactersCommand(user.UserId(), id, req), ct)));

        group.MapPatch("/{id:guid}/participants/{participantId:guid}", async (Guid id, Guid participantId,
                UpdateEncounterParticipantRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateEncounterParticipantCommand, EncounterDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new UpdateEncounterParticipantCommand(user.UserId(), id, participantId, req), ct)));

        group.MapDelete("/{id:guid}/participants/{participantId:guid}", async (Guid id, Guid participantId,
            ClaimsPrincipal user, ICommandHandler<RemoveEncounterParticipantCommand, Unit> handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new RemoveEncounterParticipantCommand(user.UserId(), id, participantId), ct);
            return Results.NoContent();
        });

        // Отправка в Game Table
        group.MapPost("/{id:guid}/send-to-table", async (Guid id, SendToTableRequest req, ClaimsPrincipal user,
                ICommandHandler<SendToGameTableCommand, GameSessionDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new SendToGameTableCommand(user.UserId(), id, req.Mode), ct)));
    }
}
