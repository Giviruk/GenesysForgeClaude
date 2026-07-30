using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Покупка или выдача скакуна (ROT-MOUNT-ITEM-01). Скакун не становится позицией инвентаря: у него
/// свой статблок, порог ран и вместимость, поэтому создаётся <see cref="CharacterMount"/>. Всё, что
/// можно проверить, проверяется до списания денег — частичной покупки не бывает.
/// </summary>
public class BuyMountHandler(IAppDbContext db) : ICommandHandler<BuyMountCommand, Guid>
{
    public async Task<Guid> Handle(BuyMountCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);

        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, command.UserId, c.System, command.CharacterId, ct: ct);
        var def = await db.MountDefs
                .Include(m => m.Skills).Include(m => m.Abilities).Include(m => m.Attacks)
                .FirstOrDefaultAsync(m =>
                    m.Id == req.MountDefId && m.System == c.System
                    && (m.OwnerUserId == null
                        || (m.OwnerUserId == command.UserId
                            && (m.HomebrewPackId == null || visiblePackIds.Contains(m.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Скакун не найден.", "mount.not_found");
        if (def.Retired && !req.Free)
            throw new DomainRuleException(
                "Эта запись выведена из активного каталога — скакуна может только выдать ведущий.",
                "mount.retired");

        if (req.PriceOverride is not null && string.IsNullOrWhiteSpace(req.OverrideReason))
            throw new DomainRuleException(
                "Для цены, назначенной вручную, нужна причина.", "trade.override_reason_required");
        if (req.PriceOverride is < 0)
            throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");
        // Торг и договорная цена — разные способы: доля от назначенной вручную цены бессмысленна.
        if (req.PriceOverride is not null && req.PricePercent is not null)
            throw new DomainRuleException(
                "Задайте один способ: долю цены или договорную цену.", "trade.purchase_mode_ambiguous");
        // Бесценный профиль обычной покупкой не берётся: цену называет ведущий, и она с причиной
        // попадает в историю (ROT-ECO-01).
        if (!req.Free && req.PriceOverride is null && def.Price is null)
            throw new DomainRuleException(
                "У этого скакуна нет обычной цены — её назначает ведущий.", "mount.priceless");

        var listedPrice = def.Price ?? 0;
        var percent = req.PricePercent ?? 100;
        var total = req.Free
            ? 0
            : req.PriceOverride is { } overridePrice
                ? TradeRules.PurchaseTotal(overridePrice, 1)
                : TradeRules.PurchaseTotal(listedPrice, 1, percent);

        var charge = StartingWallet.Charge(total, c.StartingPurchaseBudget, c.Money, c.IsCreationPhase)
            ?? throw new DomainRuleException(
                $"Недостаточно средств: нужно {total}, доступно {c.StartingPurchaseBudget + c.Money}.",
                "character.funds.insufficient");
        c.StartingPurchaseBudget -= charge.FromBudget;
        c.Money -= charge.FromMoney;

        var mount = new CharacterMount
        {
            Id = Guid.NewGuid(),
            CharacterId = c.Id,
            MountDefId = def.Id,
            MountDef = def,
            Name = (req.Name ?? "").Trim(),
            Provenance = req.Free
                ? ItemProvenance.Imported
                : charge.FromBudget > 0 ? ItemProvenance.StartingBudget : ItemProvenance.Purchased,
        };
        db.CharacterMounts.Add(mount);
        c.Mounts.Add(mount);

        var costNote = charge.Total > 0
            ? $", −{charge.Total}"
              + (charge.FromBudget > 0 ? $" (бюджет {charge.FromBudget}, монеты {charge.FromMoney})" : " монет")
            : ", без оплаты";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.MountBought,
            $"Приобретён скакун «{def.Name}»{costNote}", null,
            new
            {
                mount = def.Name, code = def.Code, cost = charge.Total,
                fromBudget = charge.FromBudget, fromMoney = charge.FromMoney,
                catalogPrice = def.Price, unitPrice = req.PriceOverride ?? listedPrice,
                priceOverride = req.PriceOverride, overrideReason = req.OverrideReason,
                free = req.Free, percent,
                mode = req.PriceOverride is not null ? "override"
                    : req.PricePercent is not null ? "haggle" : "direct",
                capacity = MountRules.Capacity(def), woundThreshold = def.WoundThreshold,
            });

        await db.SaveChangesAsync(ct);
        return mount.Id;
    }
}

/// <summary>
/// Продажа скакуна (ROT-ECO-01). Способы те же три, что у предметов, и сумму во всех случаях
/// считает сервер: поля «сколько начислить» в запросе нет.
/// </summary>
public class SellMountHandler(IAppDbContext db) : ICommandHandler<SellMountCommand, Unit>
{
    public async Task<Unit> Handle(SellMountCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var mount = c.Mounts.FirstOrDefault(m => m.Id == command.MountId)
            ?? throw new DomainRuleException("Скакун не найден.", "mount.not_found");
        var def = mount.MountDef
            ?? throw new DomainRuleException("Скакун загружен без профиля.", "mount.definition_missing");

        var modes = (req.NetSuccesses is not null ? 1 : 0)
            + (req.Percent is not null ? 1 : 0)
            + (req.PriceOverride is not null ? 1 : 0);
        if (modes > 1)
            throw new DomainRuleException(
                "Задайте один способ продажи: по проверке, долей от цены или договорной ценой.",
                "trade.sale_mode_ambiguous");
        if (req.ConditionMultiplier is not null && string.IsNullOrWhiteSpace(req.ConditionReason))
            throw new DomainRuleException(
                "Для поправки за состояние нужна причина.", "trade.condition_reason_required");
        // Груз нельзя продать вместе с транспортом: сначала его разгружают, иначе покупателю
        // достались бы чужие вещи, а в листе остались бы позиции без владельца (ROT-TRANSPORT-01).
        if (c.Items.Any(i => i.CarriedByMountId == mount.Id))
            throw new DomainRuleException(
                "Сначала снимите груз и снаряжение с транспорта.", "mount.load_not_empty");
        // Проданное тягловое животное не должно оставить повозку со ссылкой в никуда.
        foreach (var drawn in c.Mounts.Where(m => m.DrawnByMountId == mount.Id))
        {
            drawn.DrawnByMountId = null;
            drawn.DrawnBy = null;
        }

        var listedPrice = def.Price ?? 0;
        int percent;
        int bookSubtotal;

        if (req.PriceOverride is { } overridePrice)
        {
            if (string.IsNullOrWhiteSpace(req.OverrideReason))
                throw new DomainRuleException(
                    "Для договорной цены нужна причина.", "trade.override_reason_required");
            if (overridePrice < 0)
                throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");

            percent = 100;
            bookSubtotal = TradeRules.PurchaseTotal(overridePrice, 1);
        }
        else if (req.NetSuccesses is { } successes)
        {
            percent = TradeRules.ProceedsPercent(successes);
            if (percent == 0)
                throw new DomainRuleException(
                    "Проверка продажи провалена — сделка не состоялась.", "trade.sale_failed");
            bookSubtotal = TradeRules.BookSubtotal(listedPrice, 1, percent);
        }
        else
        {
            percent = req.Percent ?? 100;
            if (percent is < 0 or > 100)
                throw new DomainRuleException(
                    "Доля цены при продаже задаётся от 0 до 100 процентов.", "trade.percent_invalid");
            bookSubtotal = TradeRules.BookSubtotal(listedPrice, 1, percent);
        }

        var proceeds = TradeRules.FinalProceeds(bookSubtotal, req.ConditionMultiplier ?? 1.0);
        var refund = StartingWallet.Refund(
            proceeds, c.StartingPurchaseBudget, c.StartingEquipmentMode, c.IsCreationPhase);
        c.StartingPurchaseBudget += refund.FromBudget;
        c.Money += refund.FromMoney;

        var displayName = string.IsNullOrWhiteSpace(mount.Name) ? def.Name : mount.Name;
        c.Mounts.Remove(mount);
        db.CharacterMounts.Remove(mount);

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.MountSold,
            $"Продан скакун «{displayName}», +{proceeds}", null,
            new
            {
                mount = def.Name, code = def.Code, name = mount.Name, proceeds,
                toBudget = refund.FromBudget, toMoney = refund.FromMoney,
                listedPrice, netSuccesses = req.NetSuccesses, percent, bookSubtotal,
                mode = req.PriceOverride is not null ? "override"
                    : req.NetSuccesses is not null ? "check" : "direct",
                priceOverride = req.PriceOverride, overrideReason = req.OverrideReason,
                conditionMultiplier = req.ConditionMultiplier, conditionReason = req.ConditionReason,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Правка состояния транспорта. Раны держатся в границах профиля: сервер не хранит 20 ран у
/// скакуна с порогом 12. Груз здесь не меняется — для него отдельная атомарная команда.
/// </summary>
public class UpdateMountHandler(IAppDbContext db) : ICommandHandler<UpdateMountCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMountCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var mount = c.Mounts.FirstOrDefault(m => m.Id == command.MountId)
            ?? throw new DomainRuleException("Транспорт не найден.", "mount.not_found");
        var def = mount.MountDef
            ?? throw new DomainRuleException("Транспорт загружен без профиля.", "mount.definition_missing");

        if (req.Name is not null) mount.Name = req.Name.Trim();
        if (req.WoundsCurrent is { } wounds) mount.WoundsCurrent = MountRules.ClampWounds(def, wounds);
        if (req.IsActive is { } active) mount.IsActive = active;
        if (req.Notes is not null) mount.Notes = req.Notes;

        if (req.ClearDrawnBy)
        {
            // Отвязка тяги не трогает ни повозку, ни её груз: она просто перестаёт ехать.
            mount.DrawnByMountId = null;
            mount.DrawnBy = null;
        }
        else if (req.DrawnByMountId is { } drawnById)
        {
            if (!def.RequiresTraction)
                throw new DomainRuleException(
                    "Этому транспорту тягловое животное не нужно.", "mount.traction_not_applicable");
            if (drawnById == mount.Id)
                throw new DomainRuleException(
                    "Транспорт не может тянуть сам себя.", "mount.traction_self");

            var draft = c.Mounts.FirstOrDefault(m => m.Id == drawnById)
                ?? throw new DomainRuleException("Тягловое животное не найдено.", "mount.not_found");
            if (draft.MountDef is null || !MountRules.CanDraw(draft.MountDef))
                throw new DomainRuleException(
                    "Тянуть повозку может только скакун, которому тяга не нужна самому.",
                    "mount.traction_invalid");
            // Одно животное — одна повозка: иначе оно ехало бы в двух местах сразу.
            if (c.Mounts.Any(m => m.Id != mount.Id && m.DrawnByMountId == drawnById))
                throw new DomainRuleException(
                    "Это животное уже запряжено в другой транспорт.", "mount.traction_busy");

            mount.DrawnByMountId = drawnById;
            mount.DrawnBy = draft;
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Удаление транспорта без выручки: погиб, отпущен или заведён по ошибке. Груз при этом не
/// пропадает — он возвращается владельцу и снова считается в его переносимый вес. Терять вещи
/// молча нельзя, а держать их на несуществующем транспорте — тем более (ROT-TRANSPORT-01).
/// </summary>
public class RemoveMountHandler(IAppDbContext db) : ICommandHandler<RemoveMountCommand, Unit>
{
    public async Task<Unit> Handle(RemoveMountCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var mount = c.Mounts.FirstOrDefault(m => m.Id == command.MountId)
            ?? throw new DomainRuleException("Транспорт не найден.", "mount.not_found");

        var defName = mount.MountDef?.Name ?? "транспорт";
        var displayName = string.IsNullOrWhiteSpace(mount.Name) ? defName : mount.Name;

        var cargo = c.Items.Where(i => i.CarriedByMountId == mount.Id).ToList();
        foreach (var item in cargo)
        {
            item.CarriedByMountId = null;
            item.CarriedByMount = null;
            item.IsInstalledOnMount = false;
        }
        foreach (var drawn in c.Mounts.Where(m => m.DrawnByMountId == mount.Id))
        {
            drawn.DrawnByMountId = null;
            drawn.DrawnBy = null;
        }

        c.Mounts.Remove(mount);
        db.CharacterMounts.Remove(mount);

        var cargoNote = cargo.Count > 0 ? $"; груз ({cargo.Count} поз.) возвращён владельцу" : "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.MountRemoved,
            $"Удалён транспорт «{displayName}» без выручки{cargoNote}", null,
            new
            {
                mount = defName, code = mount.MountDef?.Code ?? "", name = mount.Name,
                releasedCargo = cargo.Count,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Перенос позиции между персонажем и транспортом (ROT-TRANSPORT-01). Одна команда в обе стороны:
/// всё проверяется до записи, поэтому наполовину перенесённой позиции не бывает. Частичный перенос
/// отделяет часть стопки в новую позицию с теми же свойствами экземпляра.
/// </summary>
public class MoveCargoHandler(IAppDbContext db) : ICommandHandler<MoveCargoCommand, Unit>
{
    public async Task<Unit> Handle(MoveCargoCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Позиция не найдена.", "item.not_found");
        var def = item.ItemDef
            ?? throw new DomainRuleException("Позиция загружена без справочника.", "item.definition_missing");

        var quantity = req.Quantity ?? Math.Max(1, item.Quantity);
        if (quantity < 1)
            throw new DomainRuleException("Количество должно быть больше нуля.", "cargo.quantity_invalid");
        if (quantity > Math.Max(1, item.Quantity))
            throw new DomainRuleException(
                "В позиции столько нет.", "cargo.quantity_exceeds_stack");

        CharacterMount? target = null;
        if (req.MountId is { } mountId)
        {
            target = c.Mounts.FirstOrDefault(m => m.Id == mountId)
                ?? throw new DomainRuleException("Транспорт не найден.", "mount.not_found");
            var targetDef = target.MountDef
                ?? throw new DomainRuleException(
                    "Транспорт загружен без профиля.", "mount.definition_missing");
            if (item.CarriedByMountId == mountId && item.IsInstalledOnMount == req.Install)
                throw new DomainRuleException("Позиция уже там.", "cargo.already_there");
            if (req.Install && !ShopCatalogRules.IsMountGear(def.Code))
                throw new DomainRuleException(
                    "Это снаряжение не устанавливается на транспорт.", "cargo.not_mount_gear");
            // Попона рассчитана на боевого скакуна. На любого другого её ставит ведущий, и причина
            // обязательна: иначе решение стола не отличить от ошибки ввода (ROT-MOUNT-NPC-01).
            if (req.Install && ShopCatalogRules.IsBarding(def.Code)
                && MountRules.RequiresGmApprovalForBarding(targetDef)
                && string.IsNullOrWhiteSpace(req.InstallOverrideReason))
                throw new DomainRuleException(
                    "Попона рассчитана на боевого скакуна — для другого нужна причина от ведущего.",
                    "cargo.barding_requires_override");

            // Вместимость считается по состоянию после переноса: своя же позиция, если она уже
            // лежит на этом транспорте, не должна учитываться дважды.
            var cargoAfter = c.Items
                .Where(i => i.CarriedByMountId == mountId && i.Id != item.Id)
                .ToList();
            if (!req.Install)
            {
                var load = MountRules.CargoLoad(cargoAfter)
                    + Math.Max(0, def.Encumbrance) * quantity;
                var capacity = MountRules.Capacity(
                    targetDef, MountRules.InstalledCapacityBonus(cargoAfter));
                if (load > capacity)
                    throw new DomainRuleException(
                        $"Не помещается: груз {load} при вместимости {capacity}.",
                        "cargo.capacity_exceeded");
            }
        }
        else if (item.CarriedByMountId is null)
        {
            throw new DomainRuleException("Позиция и так у персонажа.", "cargo.already_there");
        }

        var source = item.CarriedByMountId is { } fromId
            ? c.Mounts.FirstOrDefault(m => m.Id == fromId)
            : null;

        // Частичный перенос: остаток остаётся на месте, а уходящая часть становится своей позицией.
        CharacterItem moved;
        if (quantity < Math.Max(1, item.Quantity))
        {
            item.Quantity -= quantity;
            moved = CloneItem(item, quantity);
            c.Items.Add(moved);
            db.CharacterItems.Add(moved);
        }
        else
        {
            moved = item;
        }

        moved.CarriedByMountId = req.MountId;
        moved.CarriedByMount = target;
        moved.IsInstalledOnMount = req.MountId is not null && req.Install;
        // На транспорте предмет не надет и не в руках владельца: он лежит грузом.
        if (req.MountId is not null) moved.State = ItemState.Backpack;

        var where = target is null
            ? "владельцу"
            : $"на «{MountMapper.DisplayName(target)}»";
        var verb = req.Install ? "Установлено" : "Перенесено";
        var quantityNote = quantity > 1 ? $" ×{quantity}" : "";
        // Решение ведущего видно прямо в строке истории, а не только в данных записи.
        var overrideReason = moved.IsInstalledOnMount ? req.InstallOverrideReason?.Trim() : null;
        var overrideNote = string.IsNullOrEmpty(overrideReason)
            ? ""
            : $" (решение ведущего: {overrideReason})";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.CargoMoved,
            $"{verb} «{def.Name}»{quantityNote} {where}{overrideNote}", null,
            new
            {
                item = def.Name, code = def.Code, quantity,
                fromMount = source is null ? null : MountMapper.DisplayName(source),
                toMount = target is null ? null : MountMapper.DisplayName(target),
                installed = moved.IsInstalledOnMount,
                installOverrideReason = overrideReason,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    /// <summary>
    /// Отделённая часть стопки: у неё те же свойства экземпляра, что и у исходной позиции, иначе
    /// половина железной кольчуги стала бы стальной. Улучшения не копируются — они стоят на
    /// конкретном экземпляре.
    /// </summary>
    private static CharacterItem CloneItem(CharacterItem src, int quantity) => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = src.CharacterId,
        ItemDefId = src.ItemDefId,
        ItemDef = src.ItemDef,
        Quantity = quantity,
        State = src.State,
        Provenance = src.Provenance,
        Craftsmanship = src.Craftsmanship,
        DamageState = src.DamageState,
        ImplementMaterial = src.ImplementMaterial,
        ImplementChoices = src.ImplementChoices,
        ImplementConfigured = src.ImplementConfigured,
        ShardActivationChoice = src.ShardActivationChoice,
        ShardEffectChoice = src.ShardEffectChoice,
        ShardEffectAction = src.ShardEffectAction,
        ShardConfigured = src.ShardConfigured,
    };
}
