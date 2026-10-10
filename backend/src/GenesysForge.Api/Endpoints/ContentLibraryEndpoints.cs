using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.ContentLibrary;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace GenesysForge.Api.Endpoints;

public static class ContentLibraryEndpoints
{
    public static void MapContentLibrary(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/library", async (GameSystem? system, ClaimsPrincipal user,
            IQueryHandler<GetLibraryQuery, List<LibraryEntryDto>> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetLibraryQuery(user.UserId(), system), ct))).RequireAuthorization();

        app.MapGet("/api/library/proposals", async ( ClaimsPrincipal user,
            IQueryHandler<GetLibraryProposalsQuery, List<LibraryProposalDto>> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetLibraryProposalsQuery(user.UserId()), ct))).RequireAuthorization();

        app.MapGet("/api/reference/base-catalog", async (GameSystem system, ClaimsPrincipal user,
            IQueryHandler<GetBaseCatalogQuery, List<BaseCatalogEntryDto>> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetBaseCatalogQuery(user.UserId(), system), ct))).RequireAuthorization();

        app.MapPost("/api/homebrew-packs", async (PackDetailsRequest req, ClaimsPrincipal user,
            ICommandHandler<CreatePackCommand, HomebrewPackListItemDto> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new CreatePackCommand(user.UserId(), req), ct))).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPut("/api/homebrew-packs/{id:guid}", async (Guid id, UpdatePackDetailsRequest req, ClaimsPrincipal user,
            ICommandHandler<UpdatePackCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new UpdatePackCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/homebrew-packs/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeletePackCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeletePackCommand(user.UserId(), id), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPost("/api/homebrew-packs/{id:guid}/entries", async (Guid id, ContentEntriesRequest req, ClaimsPrincipal user,
            ICommandHandler<ChangePackEntriesCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ChangePackEntriesCommand(user.UserId(), id, req, false), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/homebrew-packs/{id:guid}/entries", async (Guid id, [FromBody] ContentEntriesRequest req, ClaimsPrincipal user,
            ICommandHandler<ChangePackEntriesCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ChangePackEntriesCommand(user.UserId(), id, req, true), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapGet("/api/homebrew-packs/{id:guid}/exclusions", async (Guid id, ClaimsPrincipal user,
            IQueryHandler<GetPackExclusionsQuery, List<BaseCatalogEntryDto>> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetPackExclusionsQuery(user.UserId(), id), ct))).RequireAuthorization();

        app.MapPost("/api/homebrew-packs/{id:guid}/exclusions", async (Guid id, PackExclusionsRequest req, ClaimsPrincipal user,
            ICommandHandler<ChangePackExclusionsCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ChangePackExclusionsCommand(user.UserId(), id, req, false), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/homebrew-packs/{id:guid}/exclusions", async (Guid id, [FromBody] PackExclusionsRequest req, ClaimsPrincipal user,
            ICommandHandler<ChangePackExclusionsCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ChangePackExclusionsCommand(user.UserId(), id, req, true), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapGet("/api/campaigns/{campaignId:guid}/content", async (Guid campaignId, ClaimsPrincipal user,
            IQueryHandler<GetCampaignContentQuery, CampaignContentDto> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetCampaignContentQuery(user.UserId(), campaignId), ct))).RequireAuthorization();

        app.MapPut("/api/campaigns/{campaignId:guid}/content/systems/{system}", async (Guid campaignId, GameSystem system, SystemOpenRequest req, ClaimsPrincipal user,
            ICommandHandler<SetCampaignSystemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetCampaignSystemCommand(user.UserId(), campaignId, system, req.IsOpen), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapGet("/api/campaigns/{campaignId:guid}/content/base", async (Guid campaignId, GameSystem system, BaseContentCategory? category, ClaimsPrincipal user,
            IQueryHandler<GetCampaignBaseQuery, List<CampaignBaseEntryDto>> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetCampaignBaseQuery(user.UserId(), campaignId, system, category), ct))).RequireAuthorization();

        app.MapPut("/api/campaigns/{campaignId:guid}/content/base", async (Guid campaignId, BaseOverridesRequest req, ClaimsPrincipal user,
            ICommandHandler<SetCampaignBaseCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetCampaignBaseCommand(user.UserId(), campaignId, req), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/campaigns/{campaignId:guid}/content/base", async (Guid campaignId, GameSystem system, ClaimsPrincipal user,
            ICommandHandler<ResetCampaignBaseCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ResetCampaignBaseCommand(user.UserId(), campaignId, system), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPost("/api/campaigns/{campaignId:guid}/content/base/save-as-pack", async (Guid campaignId, SaveRestrictionsRequest req, ClaimsPrincipal user,
            ICommandHandler<SaveCampaignRestrictionsCommand, HomebrewPackListItemDto> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new SaveCampaignRestrictionsCommand(user.UserId(), campaignId, req), ct))).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/campaigns/{campaignId:guid}/homebrew-packs/{packId:guid}", async (Guid campaignId, Guid packId, ClaimsPrincipal user,
            ICommandHandler<DisconnectCampaignPackCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DisconnectCampaignPackCommand(user.UserId(), campaignId, packId), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPut("/api/campaigns/{campaignId:guid}/homebrew-packs/{packId:guid}/entries", async (Guid campaignId, Guid packId, CampaignPackEntriesRequest req, ClaimsPrincipal user,
            ICommandHandler<SetCampaignPackEntriesCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetCampaignPackEntriesCommand(user.UserId(), campaignId, packId, req), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapGet("/api/campaigns/{campaignId:guid}/content/items", async (Guid campaignId, ClaimsPrincipal user,
            IQueryHandler<GetCampaignItemsQuery, List<CampaignContentItemDto>> handler, CancellationToken ct) =>
        Results.Ok(await handler.Handle(new GetCampaignItemsQuery(user.UserId(), campaignId), ct))).RequireAuthorization();

        app.MapPost("/api/campaigns/{campaignId:guid}/content/items", async (Guid campaignId, ContentEntriesRequest req, ClaimsPrincipal user,
            ICommandHandler<ConnectCampaignItemsCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ConnectCampaignItemsCommand(user.UserId(), campaignId, req), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPut("/api/campaigns/{campaignId:guid}/content/items/{itemId:guid}", async (Guid campaignId, Guid itemId, HomebrewPackToggleRequest req, ClaimsPrincipal user,
            ICommandHandler<SetCampaignItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetCampaignItemCommand(user.UserId(), campaignId, itemId, req.IsEnabled), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/campaigns/{campaignId:guid}/content/items/{itemId:guid}", async (Guid campaignId, Guid itemId, ClaimsPrincipal user,
            ICommandHandler<RemoveCampaignItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveCampaignItemCommand(user.UserId(), campaignId, itemId), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPost("/api/campaigns/{campaignId:guid}/content/proposals", async (Guid campaignId, ContentProposalRequest req, ClaimsPrincipal user,
            ICommandHandler<ProposeContentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ProposeContentCommand(user.UserId(), campaignId, req), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapPost("/api/campaigns/{campaignId:guid}/content/proposals/{kind}/{targetId:guid}/{decision}", async (Guid campaignId, string kind, Guid targetId, string decision, ClaimsPrincipal user,
            ICommandHandler<DecideContentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DecideContentCommand(user.UserId(), campaignId, kind, targetId, decision), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

        app.MapDelete("/api/campaigns/{campaignId:guid}/content/proposals/{kind}/{targetId:guid}", async (Guid campaignId, string kind, Guid targetId, ClaimsPrincipal user,
            ICommandHandler<WithdrawContentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new WithdrawContentCommand(user.UserId(), campaignId, kind, targetId), ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<ContentMutationNotificationFilter>();

    }
}
