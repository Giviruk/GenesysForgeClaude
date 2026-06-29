using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

public static class CharacterEndpoints
{
    public static void MapCharacters(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/share/{token}", async (string token,
                IQueryHandler<GetSharedCharacterSheetQuery, CharacterSheetDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetSharedCharacterSheetQuery(token), ct)));

        var group = app.MapGroup("/api/characters").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user,
                IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCharactersQuery(user.UserId()), ct)));

        group.MapPost("/", async (CreateCharacterRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateCharacterCommand, Guid> handler, CancellationToken ct) =>
        {
            var id = await handler.Handle(new CreateCharacterCommand(user.UserId(), req), ct);
            return Results.Created($"/api/characters/{id}", new { Id = id });
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetCharacterSheetQuery, CharacterSheetDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCharacterSheetQuery(user.UserId(), id), ct)));

        group.MapPost("/{id:guid}/duplicate", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DuplicateCharacterCommand, Guid> handler, CancellationToken ct) =>
        {
            var copyId = await handler.Handle(new DuplicateCharacterCommand(user.UserId(), id), ct);
            return Results.Created($"/api/characters/{copyId}", new { Id = copyId });
        });

        group.MapPost("/{id:guid}/share", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<CreateCharacterShareCommand, CharacterShareResponse> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCharacterShareCommand(user.UserId(), id), ct)));

        group.MapDelete("/{id:guid}/share", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<RevokeCharacterSharesCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RevokeCharacterSharesCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        // Экспорт персонажа в переносимый JSON (формат genesysforge.character.v1).
        group.MapGet("/{id:guid}/export", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<ExportCharacterQuery, CharacterExportDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ExportCharacterQuery(user.UserId(), id), ct)));

        // Импорт персонажа из JSON — всегда создаёт нового. Возвращает id и предупреждения.
        group.MapPost("/import", async (CharacterExportDto payload, ClaimsPrincipal user,
            ICommandHandler<ImportCharacterCommand, ImportCharacterResult> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new ImportCharacterCommand(user.UserId(), payload), ct);
            return Results.Created($"/api/characters/{result.CharacterId}", result);
        });

        // Предпросмотр импорта: что будет создано + предупреждения о неразрешённых ссылках. Без сохранения.
        group.MapPost("/import/preview", async (CharacterExportDto payload, ClaimsPrincipal user,
                IQueryHandler<PreviewImportCharacterQuery, ImportPreviewDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new PreviewImportCharacterQuery(user.UserId(), payload), ct)));

        group.MapPatch("/{id:guid}", async (Guid id, UpdateCharacterRequest req, ClaimsPrincipal user,
            ICommandHandler<UpdateCharacterCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new UpdateCharacterCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCharacterCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCharacterCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/complete-creation", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<CompleteCreationCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new CompleteCreationCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        // История персонажа (XP / audit log, U-09).
        group.MapGet("/{id:guid}/audit", async (Guid id, int? take, ClaimsPrincipal user,
                IQueryHandler<GetCharacterAuditQuery, IReadOnlyList<CharacterAuditEntryDto>> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCharacterAuditQuery(user.UserId(), id, take ?? 100), ct)));

        group.MapPost("/{id:guid}/xp-awards", async (Guid id, AwardXpRequest req, ClaimsPrincipal user,
            ICommandHandler<AwardXpCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new AwardXpCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate-ability", async (Guid id, ClaimsPrincipal user,
                ICommandHandler<ActivateCharacterAbilityCommand, ActivateCharacterAbilityResult> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ActivateCharacterAbilityCommand(user.UserId(), id), ct)));

        group.MapPost("/{id:guid}/characteristics/{type}/buy", async (Guid id, string type,
            ClaimsPrincipal user, ICommandHandler<BuyCharacteristicCommand, Unit> handler, CancellationToken ct) =>
        {
            // Биндинг enum из маршрута чувствителен к регистру, а фронтенд шлёт camelCase — разбираем сами.
            if (!Enum.TryParse<CharacteristicType>(type, ignoreCase: true, out var characteristic))
                throw new DomainRuleException($"Неизвестная характеристика: «{type}».");
            await handler.Handle(new BuyCharacteristicCommand(user.UserId(), id, characteristic), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/characteristics/{type}/refund", async (Guid id, string type,
            ClaimsPrincipal user, ICommandHandler<RefundCharacteristicCommand, Unit> handler, CancellationToken ct) =>
        {
            if (!Enum.TryParse<CharacteristicType>(type, ignoreCase: true, out var characteristic))
                throw new DomainRuleException($"Неизвестная характеристика: «{type}».");
            await handler.Handle(new RefundCharacteristicCommand(user.UserId(), id, characteristic), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/skills/{skillDefId:guid}/refund-rank", async (Guid id, Guid skillDefId,
            ClaimsPrincipal user, ICommandHandler<RefundSkillRankCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RefundSkillRankCommand(user.UserId(), id, skillDefId), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/talents/refund", async (Guid id, BuyTalentRequest req, ClaimsPrincipal user,
            ICommandHandler<RefundTalentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RefundTalentCommand(user.UserId(), id, req.TalentDefId), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/skills/{skillDefId:guid}/buy-rank", async (Guid id, Guid skillDefId,
            ClaimsPrincipal user, ICommandHandler<BuySkillRankCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new BuySkillRankCommand(user.UserId(), id, skillDefId), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/talents/buy", async (Guid id, BuyTalentRequest req, ClaimsPrincipal user,
            ICommandHandler<BuyTalentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new BuyTalentCommand(user.UserId(), id, req.TalentDefId, req.Characteristic), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-ability", async (Guid id, SetHeroicAbilityRequest req, ClaimsPrincipal user,
            ICommandHandler<SetHeroicAbilityCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicAbilityCommand(user.UserId(), id, req.HeroicAbilityId), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-upgrade", async (Guid id, SetHeroicUpgradeRankRequest req,
            ClaimsPrincipal user, ICommandHandler<SetHeroicUpgradeRankCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicUpgradeRankCommand(user.UserId(), id, req.Rank), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/items", async (Guid id, AddItemRequest req, ClaimsPrincipal user,
            ICommandHandler<AddItemCommand, Guid> handler, CancellationToken ct) =>
        {
            var itemId = await handler.Handle(new AddItemCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/items/{itemId}", new { Id = itemId });
        });

        group.MapPatch("/{id:guid}/items/{itemId:guid}", async (Guid id, Guid itemId, UpdateItemRequest req,
            ClaimsPrincipal user, ICommandHandler<UpdateItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new UpdateItemCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/items/{itemId:guid}/sell", async (Guid id, Guid itemId, SellItemRequest req,
            ClaimsPrincipal user, ICommandHandler<SellItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SellItemCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}/items/{itemId:guid}", async (Guid id, Guid itemId, ClaimsPrincipal user,
            ICommandHandler<RemoveItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveItemCommand(user.UserId(), id, itemId), ct);
            return Results.NoContent();
        });

        // Критические ранения (U-23): добавление (из таблицы U-11 или вручную) и снятие.
        group.MapPost("/{id:guid}/critical-injuries", async (Guid id, AddCriticalInjuryRequest req, ClaimsPrincipal user,
            ICommandHandler<AddCriticalInjuryCommand, Guid> handler, CancellationToken ct) =>
        {
            var injuryId = await handler.Handle(new AddCriticalInjuryCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/critical-injuries/{injuryId}", new { Id = injuryId });
        });

        group.MapDelete("/{id:guid}/critical-injuries/{injuryId:guid}", async (Guid id, Guid injuryId,
            ClaimsPrincipal user, ICommandHandler<RemoveCriticalInjuryCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveCriticalInjuryCommand(user.UserId(), id, injuryId), ct);
            return Results.NoContent();
        });
    }
}
