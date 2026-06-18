using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.ContentPacks;

namespace GenesysForge.Api.Endpoints;

public static class ContentPackEndpoints
{
    public static void MapContentPacks(this IEndpointRouteBuilder app)
    {
        var campaign = app.MapGroup("/api/campaigns/{campaignId:guid}/content-packs").RequireAuthorization();

        campaign.MapGet("/", async (Guid campaignId, ClaimsPrincipal user,
            IQueryHandler<GetContentPacksQuery, List<ContentPackListItemDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetContentPacksQuery(user.UserId(), campaignId), ct)));

        campaign.MapPost("/", async (Guid campaignId, CreateContentPackRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateContentPackCommand, ContentPackDetailDto> handler, CancellationToken ct) =>
        {
            var pack = await handler.Handle(new CreateContentPackCommand(user.UserId(), campaignId, req), ct);
            return Results.Created($"/api/content-packs/{pack.Id}", pack);
        });

        var group = app.MapGroup("/api/content-packs").RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetContentPackQuery, ContentPackDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetContentPackQuery(user.UserId(), id), ct)));

        group.MapPatch("/{id:guid}", async (Guid id, UpdateContentPackRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateContentPackCommand, ContentPackDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateContentPackCommand(user.UserId(), id, req), ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteContentPackCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteContentPackCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        // Записи
        group.MapPost("/{id:guid}/entries", async (Guid id, ContentPackEntryInput input, ClaimsPrincipal user,
                ICommandHandler<AddContentPackEntryCommand, ContentPackDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new AddContentPackEntryCommand(user.UserId(), id, input), ct)));

        group.MapPut("/{id:guid}/entries/{entryId:guid}", async (Guid id, Guid entryId,
                ContentPackEntryInput input, ClaimsPrincipal user,
                ICommandHandler<UpdateContentPackEntryCommand, ContentPackDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateContentPackEntryCommand(user.UserId(), id, entryId, input), ct)));

        group.MapDelete("/{id:guid}/entries/{entryId:guid}", async (Guid id, Guid entryId, ClaimsPrincipal user,
            ICommandHandler<RemoveContentPackEntryCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveContentPackEntryCommand(user.UserId(), id, entryId), ct);
            return Results.NoContent();
        });
    }
}
