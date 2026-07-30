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
        // Груз нельзя продать вместе со скакуном: сначала его разгружают, иначе позиции остались бы
        // без владельца. Полная работа с грузом — раздел «Транспорт» (ROT-TRANSPORT-01).
        if (mount.CarriedLoad > 0)
            throw new DomainRuleException(
                "Сначала снимите груз со скакуна.", "mount.load_not_empty");

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
/// Правка состояния скакуна. Раны и груз держатся в границах профиля: сервер не хранит 20 ран у
/// скакуна с порогом 12 и не принимает отрицательный груз.
/// </summary>
public class UpdateMountHandler(IAppDbContext db) : ICommandHandler<UpdateMountCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMountCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var mount = c.Mounts.FirstOrDefault(m => m.Id == command.MountId)
            ?? throw new DomainRuleException("Скакун не найден.", "mount.not_found");
        var def = mount.MountDef
            ?? throw new DomainRuleException("Скакун загружен без профиля.", "mount.definition_missing");

        if (req.Name is not null) mount.Name = req.Name.Trim();
        if (req.WoundsCurrent is { } wounds) mount.WoundsCurrent = MountRules.ClampWounds(def, wounds);
        if (req.CarriedLoad is { } load)
        {
            if (load < 0)
                throw new DomainRuleException("Груз не может быть отрицательным.", "mount.load_negative");
            mount.CarriedLoad = load;
        }
        if (req.IsActive is { } active) mount.IsActive = active;
        if (req.Notes is not null) mount.Notes = req.Notes;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Удаление скакуна без выручки: погиб, отпущен или заведён по ошибке.</summary>
public class RemoveMountHandler(IAppDbContext db) : ICommandHandler<RemoveMountCommand, Unit>
{
    public async Task<Unit> Handle(RemoveMountCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var mount = c.Mounts.FirstOrDefault(m => m.Id == command.MountId)
            ?? throw new DomainRuleException("Скакун не найден.", "mount.not_found");

        var defName = mount.MountDef?.Name ?? "скакун";
        var displayName = string.IsNullOrWhiteSpace(mount.Name) ? defName : mount.Name;
        c.Mounts.Remove(mount);
        db.CharacterMounts.Remove(mount);

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.MountRemoved,
            $"Удалён скакун «{displayName}» без выручки", null,
            new
            {
                mount = defName, code = mount.MountDef?.Code ?? "", name = mount.Name,
                carriedLoad = mount.CarriedLoad,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
