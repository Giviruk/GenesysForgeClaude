using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.HomebrewPacks;

namespace GenesysForge.Api.Endpoints;

public static class HomebrewPackEndpoints
{
    public static void MapHomebrewPacks(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/homebrew-packs").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user,
            IQueryHandler<GetHomebrewPacksQuery, List<HomebrewPackListItemDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetHomebrewPacksQuery(user.UserId()), ct)));

        group.MapGet("/{id:guid}/export", async (Guid id, ClaimsPrincipal user,
            IQueryHandler<ExportHomebrewPackQuery, HomebrewPackExportDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ExportHomebrewPackQuery(user.UserId(), id), ct)));

        group.MapPost("/import", async (HomebrewPackExportDto document, ClaimsPrincipal user,
            ICommandHandler<ImportHomebrewPackCommand, HomebrewPackImportResult> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new ImportHomebrewPackCommand(user.UserId(), document), ct);
            return Results.Created($"/api/homebrew-packs/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/share", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<ShareHomebrewPackCommand, HomebrewPackShareDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ShareHomebrewPackCommand(user.UserId(), id), ct)));

        group.MapPost("/shared/{token}/import", async (string token, ClaimsPrincipal user,
            ICommandHandler<ImportSharedHomebrewPackCommand, HomebrewPackImportResult> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new ImportSharedHomebrewPackCommand(user.UserId(), token), ct);
            return Results.Created($"/api/homebrew-packs/{result.Id}", result);
        });

        group.MapPut("/{id:guid}/default", async (Guid id, HomebrewPackToggleRequest req, ClaimsPrincipal user,
            ICommandHandler<SetHomebrewPackDefaultCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHomebrewPackDefaultCommand(user.UserId(), id, req.IsEnabled), ct);
            return Results.NoContent();
        });

        app.MapPut("/api/characters/{characterId:guid}/homebrew-packs/{packId:guid}",
            async (Guid characterId, Guid packId, HomebrewPackToggleRequest req, ClaimsPrincipal user,
                ICommandHandler<SetCharacterHomebrewPackCommand, Unit> handler, CancellationToken ct) =>
            {
                await handler.Handle(new SetCharacterHomebrewPackCommand(user.UserId(), characterId, packId, req.IsEnabled), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        app.MapPut("/api/campaigns/{campaignId:guid}/homebrew-packs/{packId:guid}",
            async (Guid campaignId, Guid packId, HomebrewPackToggleRequest req, ClaimsPrincipal user,
                ICommandHandler<SetCampaignHomebrewPackCommand, Unit> handler, CancellationToken ct) =>
            {
                await handler.Handle(new SetCampaignHomebrewPackCommand(user.UserId(), campaignId, packId, req.IsEnabled), ct);
                return Results.NoContent();
            }).RequireAuthorization();
    }
}
