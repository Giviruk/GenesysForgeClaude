using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
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
        // Клиент может попросить вернуть обновлённый лист прямо в ответе на правку и не ходить за
        // ним вторым запросом (см. ReturnSheetFilter). Без заголовка поведение прежнее.
        group.AddEndpointFilter(ReturnSheetFilter.Apply);

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

        // Лист по частям: `?include=base,items`. Вкладка берёт свою часть и не платит за чужие —
        // у играющего персонажа один инвентарь весит вдвое больше всего остального листа.
        // Полный `GET /{id}` остаётся: печати, экспорту и публичной ссылке нужно всё сразу.
        group.MapGet("/{id:guid}/slices", async (Guid id, string? include, ClaimsPrincipal user,
                IQueryHandler<GetCharacterSlicesQuery, SheetSlicesDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(
                new GetCharacterSlicesQuery(user.UserId(), id, SheetSlices.Parse(include)), ct)));

        group.MapPost("/{id:guid}/duplicate", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DuplicateCharacterCommand, Guid> handler, CancellationToken ct) =>
        {
            var copyId = await handler.Handle(new DuplicateCharacterCommand(user.UserId(), id), ct);
            return Results.Created($"/api/characters/{copyId}", new { Id = copyId });
        });

        // Портрет загружается сырым телом запроса; формат и размер проверяются по содержимому.
        // Под rate limit, чтобы загрузками нельзя было забить хранилище.
        group.MapPost("/{id:guid}/portrait", async (Guid id, HttpRequest request, ClaimsPrincipal user,
            ICommandHandler<UploadCharacterPortraitCommand, string> handler, CancellationToken ct) =>
        {
            var content = await UploadBody.ReadImageAsync(request, ct);
            var url = await handler.Handle(new UploadCharacterPortraitCommand(user.UserId(), id, content), ct);
            return Results.Ok(new { PortraitUrl = url });
        }).RequireRateLimiting(AuthRateLimiting.SessionPolicy);

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

        group.MapPost("/{id:guid}/audit/{entryId:guid}/undo", async (Guid id, Guid entryId,
                ClaimsPrincipal user, ICommandHandler<UndoCharacterAuditCommand, Unit> handler,
                CancellationToken ct) =>
        {
            await handler.Handle(new UndoCharacterAuditCommand(user.UserId(), id, entryId), ct);
            return Results.NoContent();
        });

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
            await handler.Handle(new BuyTalentCommand(user.UserId(), id, req.TalentDefId, req.Characteristic, req.Choices), ct);
            return Results.NoContent();
        });

        // Метнуть оружие или подобрать его обратно (ROT-WPN-01).
        group.MapPut("/{id:guid}/items/{itemId:guid}/thrown", async (Guid id, Guid itemId,
            SetItemThrownRequest req, ClaimsPrincipal user,
            ICommandHandler<SetItemThrownCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetItemThrownCommand(user.UserId(), id, itemId, req.IsThrown), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-ability", async (Guid id, SetHeroicAbilityRequest req, ClaimsPrincipal user,
            ICommandHandler<SetHeroicAbilityCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicAbilityCommand(user.UserId(), id, req.HeroicAbilityId), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-identity", async (Guid id, SetHeroicIdentityRequest req,
            ClaimsPrincipal user, ICommandHandler<SetHeroicIdentityCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicIdentityCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/heroic-identity/roll-origin", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<RollHeroicOriginCommand, HeroicOriginRollDto> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new RollHeroicOriginCommand(user.UserId(), id), ct);
            return Results.Ok(result);
        });

        group.MapPut("/{id:guid}/heroic-configuration", async (Guid id, SetHeroicConfigurationRequest req,
            ClaimsPrincipal user, ICommandHandler<SetHeroicConfigurationCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicConfigurationCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/heroic-configuration/signature-weapon", async (Guid id,
            ReplaceSignatureWeaponRequest req, ClaimsPrincipal user,
            ICommandHandler<ReplaceSignatureWeaponCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ReplaceSignatureWeaponCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        // Выбор Improved/Supreme именного оружия (ROT-HA-05): отдельная команда, потому что сам
        // параметр оружия после создания неизменяем, а эти решения приходят с покупкой улучшений.
        group.MapPost("/{id:guid}/heroic-configuration/signature-weapon/upgrades", async (Guid id,
            SetSignatureWeaponUpgradesRequest req, ClaimsPrincipal user,
            ICommandHandler<SetSignatureWeaponUpgradesCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetSignatureWeaponUpgradesCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-upgrade", async (Guid id, SetHeroicUpgradeRankRequest req,
            ClaimsPrincipal user, ICommandHandler<SetHeroicUpgradeRankCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicUpgradeRankCommand(user.UserId(), id, req.Rank), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-upgrades", async (Guid id, SetHeroicUpgradesRequest req,
            ClaimsPrincipal user, ICommandHandler<SetHeroicUpgradesCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicUpgradesCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/items", async (Guid id, AddItemRequest req, ClaimsPrincipal user,
            ICommandHandler<AddItemCommand, Guid> handler, CancellationToken ct) =>
        {
            var itemId = await handler.Handle(new AddItemCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/items/{itemId}", new CreatedInCharacterResponse(itemId));
        });

        group.MapPost("/{id:guid}/services", async (Guid id, BuyServiceRequest req,
            ClaimsPrincipal user, ICommandHandler<BuyServiceCommand, Unit> handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new BuyServiceCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        // Транспорт (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Покупка создаёт экземпляр со статблоком,
        // поэтому это не /items: у транспорта свой порог ран и своя вместимость, а в Encumbrance
        // владельца он не входит.
        group.MapPost("/{id:guid}/mounts", async (Guid id, BuyMountRequest req, ClaimsPrincipal user,
            ICommandHandler<BuyMountCommand, Guid> handler, CancellationToken ct) =>
        {
            var mountId = await handler.Handle(new BuyMountCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/mounts/{mountId}", new CreatedInCharacterResponse(mountId));
        });

        group.MapPatch("/{id:guid}/mounts/{mountId:guid}", async (Guid id, Guid mountId,
            UpdateMountRequest req, ClaimsPrincipal user,
            ICommandHandler<UpdateMountCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new UpdateMountCommand(user.UserId(), id, mountId, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/mounts/{mountId:guid}/sell", async (Guid id, Guid mountId,
            SellMountRequest req, ClaimsPrincipal user,
            ICommandHandler<SellMountCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SellMountCommand(user.UserId(), id, mountId, req), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}/mounts/{mountId:guid}", async (Guid id, Guid mountId,
            ClaimsPrincipal user, ICommandHandler<RemoveMountCommand, Unit> handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new RemoveMountCommand(user.UserId(), id, mountId), ct);
            return Results.NoContent();
        });

        // Изготовление, варка и зачарование (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01).
        // Доступны владельцу листа — и игроку, и ведущему: отдельного gm-режима у ремесла нет.
        group.MapGet("/{id:guid}/crafting", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetCraftingProjectsQuery, List<CraftingProjectDto>> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCraftingProjectsQuery(user.UserId(), id), ct)));

        // Предпросмотр ничего не пишет: сложность, время и стоимость видны до подтверждения.
        group.MapPost("/{id:guid}/crafting/preview", async (Guid id, CraftingProjectInput req,
                ClaimsPrincipal user, IQueryHandler<PreviewCraftingQuery, CraftingPreviewDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new PreviewCraftingQuery(user.UserId(), id, req), ct)));

        group.MapPost("/{id:guid}/crafting", async (Guid id, CraftingProjectInput req,
            ClaimsPrincipal user, ICommandHandler<StartCraftingCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var projectId = await handler.Handle(new StartCraftingCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/crafting/{projectId}",
                new CreatedInCharacterResponse(projectId));
        });

        group.MapPost("/{id:guid}/crafting/{projectId:guid}/resolve", async (Guid id, Guid projectId,
                CraftingResolveInput req, ClaimsPrincipal user,
                ICommandHandler<ResolveCraftingCommand, CraftingProjectDto> handler,
                CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ResolveCraftingCommand(user.UserId(), id, projectId, req), ct)));

        group.MapDelete("/{id:guid}/crafting/{projectId:guid}", async (Guid id, Guid projectId,
            ClaimsPrincipal user, ICommandHandler<CancelCraftingCommand, Unit> handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new CancelCraftingCommand(user.UserId(), id, projectId), ct);
            return Results.NoContent();
        });

        // Груз персонаж ⇄ транспорт одной командой в обе стороны (ROT-TRANSPORT-01): это правка
        // места хранения позиции, поэтому маршрут висит на предмете, а не на транспорте.
        group.MapPatch("/{id:guid}/items/{itemId:guid}/location", async (Guid id, Guid itemId,
            MoveCargoRequest req, ClaimsPrincipal user,
            ICommandHandler<MoveCargoCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new MoveCargoCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
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

        // Улучшения предметов (ROT-EQP-ATT-01). Установка идёт по кнопке: броска проверки нет,
        // правило книги показывается подсказкой в интерфейсе.
        group.MapPost("/{id:guid}/attachments", async (Guid id, BuyAttachmentRequest req, ClaimsPrincipal user,
            ICommandHandler<BuyAttachmentCommand, Guid> handler, CancellationToken ct) =>
        {
            var attachmentId = await handler.Handle(new BuyAttachmentCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/attachments/{attachmentId}", new CreatedInCharacterResponse(attachmentId));
        });

        group.MapPost("/{id:guid}/attachments/install", async (Guid id, InstallAttachmentRequest req,
            ClaimsPrincipal user, ICommandHandler<InstallAttachmentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new InstallAttachmentCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/attachments/{attachmentId:guid}/detach", async (Guid id, Guid attachmentId,
            DetachAttachmentRequest req, ClaimsPrincipal user,
            ICommandHandler<DetachAttachmentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DetachAttachmentCommand(user.UserId(), id, attachmentId, req), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId,
            ClaimsPrincipal user, ICommandHandler<RemoveAttachmentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveAttachmentCommand(user.UserId(), id, attachmentId), ct);
            return Results.NoContent();
        });

        // Состояние повреждения и ремонт (GEN-EQP-DMG-01). Состояние меняется отдельным действием,
        // ремонт идёт по кнопке: броска проверки нет, правило книги показано памяткой в интерфейсе.
        group.MapPut("/{id:guid}/items/{itemId:guid}/damage-state", async (Guid id, Guid itemId,
            SetItemDamageStateRequest req, ClaimsPrincipal user,
            ICommandHandler<SetItemDamageStateCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetItemDamageStateCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/items/{itemId:guid}/repair", async (Guid id, Guid itemId,
            RepairItemRequest? req, ClaimsPrincipal user,
            ICommandHandler<RepairItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RepairItemCommand(user.UserId(), id, itemId, req ?? new()), ct);
            return Results.NoContent();
        });

        // Настройка магического инструмента ведущим (ROT-MAG-IMP-01): выбор делается один раз.
        group.MapPut("/{id:guid}/items/{itemId:guid}/implement", async (Guid id, Guid itemId,
            SetImplementConfigurationRequest req, ClaimsPrincipal user,
            ICommandHandler<SetImplementConfigurationCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetImplementConfigurationCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/items/{itemId:guid}/lesser-rune", async (Guid id, Guid itemId,
            SetLesserRuneConfigurationRequest req, ClaimsPrincipal user,
            ICommandHandler<SetLesserRuneConfigurationCommand, Unit> handler,
            CancellationToken ct) =>
        {
            await handler.Handle(
                new SetLesserRuneConfigurationCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/attachments/{attachmentId:guid}/damage-state", async (Guid id,
            Guid attachmentId, SetItemDamageStateRequest req, ClaimsPrincipal user,
            ICommandHandler<SetAttachmentDamageStateCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(
                new SetAttachmentDamageStateCommand(user.UserId(), id, attachmentId, req), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/attachments/{attachmentId:guid}/repair", async (Guid id,
            Guid attachmentId, RepairItemRequest? req, ClaimsPrincipal user,
            ICommandHandler<RepairAttachmentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(
                new RepairAttachmentCommand(user.UserId(), id, attachmentId, req ?? new()), ct);
            return Results.NoContent();
        });

        // Критические ранения (U-23): добавление (из таблицы U-11 или вручную) и снятие.
        group.MapPost("/{id:guid}/critical-injuries", async (Guid id, AddCriticalInjuryRequest req, ClaimsPrincipal user,
            ICommandHandler<AddCriticalInjuryCommand, Guid> handler, CancellationToken ct) =>
        {
            var injuryId = await handler.Handle(new AddCriticalInjuryCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/critical-injuries/{injuryId}", new CreatedInCharacterResponse(injuryId));
        });

        group.MapDelete("/{id:guid}/critical-injuries/{injuryId:guid}", async (Guid id, Guid injuryId,
            ClaimsPrincipal user, ICommandHandler<RemoveCriticalInjuryCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveCriticalInjuryCommand(user.UserId(), id, injuryId), ct);
            return Results.NoContent();
        });
    }
}
