using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.GameTable;

namespace GenesysForge.Api.Endpoints;

public static class RollEndpoints
{
    public static void MapRolls(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/campaigns/{campaignId:guid}/rolls").RequireAuthorization();

        group.MapGet("/", async (Guid campaignId, int? take, ClaimsPrincipal user,
                IQueryHandler<GetRollsQuery, IReadOnlyList<RollLogEntryDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetRollsQuery(user.UserId(), campaignId, take ?? 30), ct)));

        group.MapPost("/", async (Guid campaignId, CreateRollRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateRollCommand, RollLogEntryDto> handler, ICampaignNotifier notifier,
            CancellationToken ct) =>
        {
            var entry = await handler.Handle(new CreateRollCommand(user.UserId(), campaignId, req), ct);
            // Realtime: участники стола перечитывают лог бросков (REST — источник истины).
            await notifier.RollAddedAsync(campaignId);
            return Results.Created($"/api/campaigns/{campaignId}/rolls/{entry.Id}", entry);
        });
    }
}
