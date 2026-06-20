using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Api.Endpoints;

public static class CampaignEndpoints
{
    public static void MapCampaigns(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/campaigns").RequireAuthorization();

        // Мутации состава/заметок конкретной кампании ({id}) → realtime-уведомление подписчикам.
        group.AddEndpointFilter(async (ctx, next) =>
        {
            var result = await next(ctx);
            if (ctx.HttpContext.Request.Method != HttpMethods.Get &&
                ctx.HttpContext.Request.RouteValues["id"] is string cid &&
                Guid.TryParse(cid, out var campaignId))
            {
                await ctx.HttpContext.RequestServices
                    .GetRequiredService<ICampaignNotifier>().CampaignChangedAsync(campaignId);
            }
            return result;
        });

        group.MapGet("/", async (ClaimsPrincipal user,
                IQueryHandler<GetCampaignsQuery, List<CampaignListItemDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCampaignsQuery(user.UserId()), ct)));

        group.MapPost("/", async (CreateCampaignRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateCampaignCommand, CampaignDetailDto> handler, CancellationToken ct) =>
        {
            var campaign = await handler.Handle(new CreateCampaignCommand(user.UserId(), req), ct);
            return Results.Created($"/api/campaigns/{campaign.Id}", campaign);
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetCampaignQuery, CampaignDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCampaignQuery(user.UserId(), id), ct)));

        group.MapPost("/join", async (JoinCampaignRequest req, ClaimsPrincipal user,
            ICommandHandler<JoinCampaignCommand, CampaignDetailDto> handler, ICampaignNotifier notifier,
            CancellationToken ct) =>
        {
            var campaign = await handler.Handle(new JoinCampaignCommand(user.UserId(), req), ct);
            await notifier.CampaignChangedAsync(campaign.Id); // GM увидит нового участника
            return Results.Ok(campaign);
        });

        group.MapDelete("/{id:guid}/characters/{characterId:guid}", async (Guid id, Guid characterId,
            ClaimsPrincipal user, ICommandHandler<RemoveCampaignCharacterCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveCampaignCharacterCommand(user.UserId(), id, characterId), ct);
            return Results.NoContent();
        });

        // Заметки кампании — только GM
        group.MapPost("/{id:guid}/notes", async (Guid id, SaveCampaignNoteRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCampaignNoteCommand, CampaignNoteDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCampaignNoteCommand(user.UserId(), id, req), ct)));

        group.MapPut("/{id:guid}/notes/{noteId:guid}", async (Guid id, Guid noteId, SaveCampaignNoteRequest req,
                ClaimsPrincipal user, ICommandHandler<UpdateCampaignNoteCommand, CampaignNoteDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCampaignNoteCommand(user.UserId(), id, noteId, req), ct)));

        group.MapDelete("/{id:guid}/notes/{noteId:guid}", async (Guid id, Guid noteId, ClaimsPrincipal user,
            ICommandHandler<DeleteCampaignNoteCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCampaignNoteCommand(user.UserId(), id, noteId), ct);
            return Results.NoContent();
        });
    }
}
