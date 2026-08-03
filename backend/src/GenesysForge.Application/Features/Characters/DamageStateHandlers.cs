using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Смена состояния повреждения предмета (GEN-EQP-DMG-01).</summary>
public record SetItemDamageStateCommand(
    Guid UserId, Guid CharacterId, Guid CharacterItemId, SetItemDamageStateRequest Request)
    : ICommand<Unit>;

/// <summary>Ремонт предмета по кнопке: материалы списываются, состояние становится целым.</summary>
public record RepairItemCommand(
    Guid UserId, Guid CharacterId, Guid CharacterItemId, RepairItemRequest Request) : ICommand<Unit>;

/// <summary>Смена состояния повреждения улучшения (GEN-EQP-DMG-01).</summary>
public record SetAttachmentDamageStateCommand(
    Guid UserId, Guid CharacterId, Guid AttachmentId, SetItemDamageStateRequest Request)
    : ICommand<Unit>;

/// <summary>Ремонт улучшения по кнопке.</summary>
public record RepairAttachmentCommand(
    Guid UserId, Guid CharacterId, Guid AttachmentId, RepairItemRequest Request) : ICommand<Unit>;

/// <summary>
/// Состояние предмета меняется отдельным действием, а не как побочный эффект чего-то ещё
/// (GEN-EQP-DMG-01). Sunder в бою, падение с обрыва и решение ведущего приводят к одной и той же
/// записи: приложение не угадывает причину, но записывает её в историю.
///
/// Укреплённый экземпляр (Ancient) не поддаётся Sunder, но это не защита от всего на свете, и
/// ручную смену состояния приложение не запрещает: за столом бывает пожар и кислота.
/// </summary>
public class SetItemDamageStateHandler(IAppDbContext db)
    : ICommandHandler<SetItemDamageStateCommand, Unit>
{
    public async Task<Unit> Handle(SetItemDamageStateCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.CharacterItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.", "item.not_found");

        var state = command.Request.State;
        DamageStateRules.EnsureKnown(state);
        SetItemDamageStateHandler.EnsureBreakable(item);

        var previous = item.DamageState;
        if (previous == state) return Unit.Value; // Повтор ничего не меняет и ошибкой не является.

        item.DamageState = state;

        // Сломанная броня перестаёт быть активной: она не даёт ни поглощения, ни защиты, и
        // держать выбор на ней значило бы прятать от игрока, что защиты у него больше нет.
        if (!DamageStateRules.IsUsable(state) && c.ActiveArmorCharacterItemId == item.Id)
            c.ActiveArmorCharacterItemId = null;

        var name = item.ItemDef?.NameRu ?? item.ItemDef?.Name ?? "";
        var reason = command.Request.Reason?.Trim();
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemDamageStateChanged,
            $"Состояние «{name}»: {DamageStateRules.NameRu(previous)} → {DamageStateRules.NameRu(state)}",
            null,
            new
            {
                characterItemId = item.Id, name,
                from = previous.ToString(), to = state.ToString(), reason,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    /// <summary>
    /// Ступени состояния описаны для того, что несут в бою (GEN-EQP-DMG-01): у оружия отваливается
    /// урон, у брони — поглощение. Снаряжению ломаться в правилах нечем, и «порог поломки» у мотка
    /// верёвки был просто шумом на карточке.
    /// </summary>
    internal static void EnsureBreakable(CharacterItem item)
    {
        if (item.ItemDef is { } def && !ItemUseRules.CanBeDamaged(def))
            throw new DomainRuleException(
                "У этого предмета нет состояния поломки.", "item.not_breakable");
    }
}

/// <summary>
/// Ремонт предмета (GEN-EQP-DMG-01). Проверки Механики приложение не бросает — решение владельца,
/// то же самое, что и у установки улучшений: правило книги показано памяткой, а исход определяет
/// стол. Кнопка делает ровно две вещи, которые считаются однозначно: списывает материалы и
/// возвращает предмету целое состояние.
///
/// Момент списания книга не фиксирует. <c>ProductDecision</c>: материалы списываются в момент
/// попытки, то есть при нажатии кнопки. Так как броска нет, попытка и успех совпадают.
/// </summary>
public class RepairItemHandler(IAppDbContext db) : ICommandHandler<RepairItemCommand, Unit>
{
    public async Task<Unit> Handle(RepairItemCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.CharacterItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.", "item.not_found");
        SetItemDamageStateHandler.EnsureBreakable(item);

        // Цена экземпляра, а не строки каталога: качество изготовления в ней учтено, цена
        // установленных улучшений — нет (они чинятся отдельно).
        var instancePrice = EffectiveItems.For(c, item).Price;
        var name = item.ItemDef?.NameRu ?? item.ItemDef?.Name ?? "";

        var previous = item.DamageState;
        var cost = RepairCharge.Apply(c, previous, instancePrice, command.Request);
        item.DamageState = ItemDamageState.Undamaged;

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemRepaired,
            $"Починен «{name}» ({DamageStateRules.NameRu(previous)})"
            + (cost > 0 ? $", материалы −{cost}" : ""),
            null,
            new
            {
                characterItemId = item.Id, name, from = previous.ToString(),
                materialCost = cost, instancePrice,
                materialPercent = DamageStateRules.MaterialPercent(previous),
                netAdvantages = command.Request.NetAdvantages,
                costOverride = command.Request.CostOverride,
                overrideReason = command.Request.OverrideReason,
                free = command.Request.Free,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Состояние улучшения (GEN-EQP-DMG-01). Сломанное улучшение перестаёт работать, но слот носителя
/// не освобождает: слот освобождает снятие, а не повреждение.
/// </summary>
public class SetAttachmentDamageStateHandler(IAppDbContext db)
    : ICommandHandler<SetAttachmentDamageStateCommand, Unit>
{
    public async Task<Unit> Handle(
        SetAttachmentDamageStateCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var attachment = c.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId)
            ?? throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");

        var state = command.Request.State;
        DamageStateRules.EnsureKnown(state);

        var previous = attachment.DamageState;
        if (previous == state) return Unit.Value;

        attachment.DamageState = state;

        var name = attachment.AttachmentDef?.NameRu ?? attachment.AttachmentDef?.Name ?? "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemDamageStateChanged,
            $"Состояние улучшения «{name}»: "
            + $"{DamageStateRules.NameRu(previous)} → {DamageStateRules.NameRu(state)}",
            null,
            new
            {
                attachmentId = attachment.Id, attachment = name,
                from = previous.ToString(), to = state.ToString(),
                reason = command.Request.Reason?.Trim(),
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Ремонт улучшения теми же правилами, что и предмета: своя цена, свои материалы.</summary>
public class RepairAttachmentHandler(IAppDbContext db) : ICommandHandler<RepairAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(RepairAttachmentCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var attachment = c.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId)
            ?? throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");

        var name = attachment.AttachmentDef?.NameRu ?? attachment.AttachmentDef?.Name ?? "";
        var previous = attachment.DamageState;
        var cost = RepairCharge.Apply(
            c, previous, attachment.AttachmentDef?.Price, command.Request);
        attachment.DamageState = ItemDamageState.Undamaged;

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemRepaired,
            $"Починено улучшение «{name}» ({DamageStateRules.NameRu(previous)})"
            + (cost > 0 ? $", материалы −{cost}" : ""),
            null,
            new
            {
                attachmentId = attachment.Id, attachment = name, from = previous.ToString(),
                materialCost = cost, instancePrice = attachment.AttachmentDef?.Price,
                materialPercent = DamageStateRules.MaterialPercent(previous),
                netAdvantages = command.Request.NetAdvantages,
                costOverride = command.Request.CostOverride,
                overrideReason = command.Request.OverrideReason,
                free = command.Request.Free,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Общая часть обоих ремонтов: проверка состояния, расчёт стоимости материалов и списание.
/// Всё проверяется до первой мутации — частично починенного предмета быть не должно.
/// </summary>
internal static class RepairCharge
{
    /// <summary>Считает и списывает материалы; возвращает фактически списанную сумму.</summary>
    /// <param name="basePrice">
    /// Цена экземпляра для расчёта материалов; <c>null</c> — обычной цены нет и нужна цена ведущего.
    /// </param>
    public static int Apply(Character c, ItemDamageState state, int? basePrice, RepairItemRequest req)
    {
        if (state == ItemDamageState.Undamaged)
            throw new DomainRuleException("Этот предмет цел — чинить нечего.", "item.repair.undamaged");
        if (state == ItemDamageState.Destroyed)
            throw new DomainRuleException(
                "Уничтоженное обычным ремонтом не чинится — дальше решает ведущий.",
                "item.repair.destroyed");
        if (req.NetAdvantages < 0)
            throw new DomainRuleException(
                "Число преимуществ не может быть отрицательным.", "item.repair.advantages_negative");
        if (req.CostOverride is < 0)
            throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");
        if (req.CostOverride is not null && string.IsNullOrWhiteSpace(req.OverrideReason))
            throw new DomainRuleException(
                "Для стоимости, назначенной вручную, нужна причина.", "trade.override_reason_required");
        if (!req.Free && req.CostOverride is null && basePrice is null)
            throw new DomainRuleException(
                "У этой записи нет обычной цены — стоимость материалов называет ведущий.",
                "item.repair.priceless");

        var total = req.Free
            ? 0
            : req.CostOverride
                ?? DamageStateRules.MaterialCost(basePrice ?? 0, state, req.NetAdvantages);

        var charge = StartingWallet.Charge(total, c.StartingPurchaseBudget, c.Money, c.IsCreationPhase)
            ?? throw new DomainRuleException(
                $"Недостаточно средств на материалы: нужно {total}, "
                + $"доступно {c.StartingPurchaseBudget + c.Money}.",
                "character.funds.insufficient");
        c.StartingPurchaseBudget -= charge.FromBudget;
        c.Money -= charge.FromMoney;
        return charge.Total;
    }
}
