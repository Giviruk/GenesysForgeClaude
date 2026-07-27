using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class AddItemHandler(IAppDbContext db) : ICommandHandler<AddItemCommand, Guid>
{
    public async Task<Guid> Handle(AddItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, command.UserId, c.System, command.CharacterId, ct: ct);
        var itemDef = await db.ItemDefs.FirstOrDefaultAsync(i =>
                i.Id == req.ItemDefId && i.System == c.System
                && (i.OwnerUserId == null
                    || (i.OwnerUserId == command.UserId
                        && (i.HomebrewPackId == null || visiblePackIds.Contains(i.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Предмет не найден.");
        if (req.Quantity < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");

        // Цену считает сервер по каталогу (ROT-ECO-01): присланная клиентом сумма не используется.
        // Цена ведущего допустима, но только явная и с причиной — она попадает в историю.
        if (req.PriceOverride is not null && string.IsNullOrWhiteSpace(req.OverrideReason))
            throw new DomainRuleException(
                "Для цены, назначенной вручную, нужна причина.", "trade.override_reason_required");
        if (req.PriceOverride is < 0)
            throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");

        // Качество изготовления экземпляра (ROT-WPN-02): у уникальных записей его задаёт каталог,
        // иначе — выбор игрока. Дальше оно неизменно, поэтому проверяется здесь и только здесь.
        var craftsmanship = CraftsmanshipRules.FixedFor(itemDef.Code) ?? req.Craftsmanship;
        CraftsmanshipRules.EnsureApplicable(itemDef.Kind, craftsmanship);

        var unitPrice = req.PriceOverride ?? CraftsmanshipRules.Price(itemDef.Price, craftsmanship);
        var total = req.Free ? 0 : TradeRules.PurchaseTotal(unitPrice, req.Quantity);

        // Покупка: сначала бюджет стартовых покупок (только в фазе создания), затем кошелёк.
        var charge = StartingWallet.Charge(total, c.StartingPurchaseBudget, c.Money, c.IsCreationPhase)
            ?? throw new DomainRuleException(
                $"Недостаточно средств: нужно {total}, доступно {c.StartingPurchaseBudget + c.Money} "
                + $"(бюджет создания {c.StartingPurchaseBudget}, монеты {c.Money}).",
                "character.funds.insufficient");
        c.StartingPurchaseBudget -= charge.FromBudget;
        c.Money -= charge.FromMoney;

        var item = new CharacterItem
        {
            Id = Guid.NewGuid(), CharacterId = c.Id, ItemDefId = itemDef.Id, ItemDef = itemDef,
            Quantity = req.Quantity, State = req.State, Craftsmanship = craftsmanship,
            Provenance = req.Free
                ? ItemProvenance.Imported
                : charge.FromBudget > 0 ? ItemProvenance.StartingBudget : ItemProvenance.Purchased,
        };
        db.CharacterItems.Add(item);
        c.Items.Add(item);
        // Первая надетая броня становится активной сама; вторая молча выбор не переключает (ROT-CMB-02).
        if (item.State == ItemState.Equipped && itemDef.Kind == ItemKind.Armor
            && c.ActiveArmorCharacterItemId is null)
            c.ActiveArmorCharacterItemId = item.Id;

        var costNote = charge.Total > 0
            ? $", −{charge.Total}" + (charge.FromBudget > 0 ? $" (бюджет {charge.FromBudget}, монеты {charge.FromMoney})" : " монет")
            : "";
        var qtyNote = req.Quantity > 1 ? $" ×{req.Quantity}" : "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemBought,
            $"Добавлен предмет «{itemDef.Name}»{qtyNote}{costNote}", null,
            new
            {
                item = itemDef.Name, quantity = req.Quantity, cost = charge.Total,
                fromBudget = charge.FromBudget, fromMoney = charge.FromMoney,
                listedUnitPrice = itemDef.Price, unitPrice, priceOverride = req.PriceOverride,
                overrideReason = req.OverrideReason, free = req.Free,
                craftsmanship = craftsmanship.ToString(),
            });

        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}
