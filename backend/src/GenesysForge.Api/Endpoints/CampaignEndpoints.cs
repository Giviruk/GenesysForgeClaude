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

        // GM или игрок открывает read-only лист персонажа своей кампании.
        group.MapGet("/{id:guid}/characters/{characterId:guid}/sheet", async (Guid id, Guid characterId,
                ClaimsPrincipal user, IQueryHandler<GetCampaignMemberSheetQuery, CharacterSheetDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCampaignMemberSheetQuery(user.UserId(), id, characterId), ct)));

        group.MapGet("/{id:guid}/characters/{characterId:guid}/audit", async (Guid id, Guid characterId,
                int? take, ClaimsPrincipal user,
                IQueryHandler<GetCampaignMemberAuditQuery, IReadOnlyList<CharacterAuditEntryDto>> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new GetCampaignMemberAuditQuery(user.UserId(), id, characterId, take ?? 100), ct)));

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

        // Хроника совместная: читать и редактировать могут GM и участники кампании.
        group.MapGet("/{id:guid}/chronicle", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetCampaignChronicleQuery, List<CampaignChronicleChapterDto>> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCampaignChronicleQuery(user.UserId(), id), ct)));

        group.MapPost("/{id:guid}/chronicle/chapters", async (Guid id,
                SaveCampaignChronicleChapterRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCampaignChronicleChapterCommand, CampaignChronicleChapterDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new CreateCampaignChronicleChapterCommand(user.UserId(), id, req), ct)));

        // Изображения отправляются сырым телом и проверяются по сигнатуре, а не по имени файла.
        group.MapPost("/{id:guid}/chronicle/images", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            ICommandHandler<UploadCampaignChronicleImageCommand, string> handler, CancellationToken ct) =>
        {
            var content = await UploadBody.ReadImageAsync(request, ct);
            var url = await handler.Handle(
                new UploadCampaignChronicleImageCommand(user.UserId(), id, content), ct);
            return Results.Ok(new { ImageUrl = url });
        }).RequireRateLimiting(AuthRateLimiting.SessionPolicy);

        group.MapPut("/{id:guid}/chronicle/chapters/{chapterId:guid}", async (Guid id, Guid chapterId,
                SaveCampaignChronicleChapterRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCampaignChronicleChapterCommand, CampaignChronicleChapterDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new UpdateCampaignChronicleChapterCommand(user.UserId(), id, chapterId, req), ct)));

        group.MapDelete("/{id:guid}/chronicle/chapters/{chapterId:guid}", async (Guid id, Guid chapterId,
            ClaimsPrincipal user, ICommandHandler<DeleteCampaignChronicleChapterCommand, Unit> handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCampaignChronicleChapterCommand(user.UserId(), id, chapterId), ct);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/chronicle/chapters/{chapterId:guid}/history", async (Guid id, Guid chapterId,
                ClaimsPrincipal user,
                IQueryHandler<GetCampaignChronicleHistoryQuery, List<CampaignChronicleRevisionDto>> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new GetCampaignChronicleHistoryQuery(user.UserId(), id, chapterId), ct)));

        group.MapPost("/{id:guid}/chronicle/chapters/{chapterId:guid}/restore/{revisionId:guid}",
            async (Guid id, Guid chapterId, Guid revisionId, ClaimsPrincipal user,
                ICommandHandler<RestoreCampaignChronicleRevisionCommand, CampaignChronicleChapterDto> handler,
                CancellationToken ct) =>
                Results.Ok(await handler.Handle(
                    new RestoreCampaignChronicleRevisionCommand(user.UserId(), id, chapterId, revisionId), ct)));
    }
}
