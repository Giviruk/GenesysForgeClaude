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
        if (RuneboundShardRules.IsShard(itemDef.Code) && req.Quantity != 1)
            throw new DomainRuleException(
                "Runebound shard хранится отдельным экземпляром; добавьте одну руну за раз.",
                "shard.quantity_one");
        if (!req.Free && !itemDef.Purchasable)
            throw new DomainRuleException(
                "У этого предмета нет обычной цены — его может только выдать ведущий.",
                "item.not_purchasable");

        // Цену считает сервер по каталогу (ROT-ECO-01): присланная клиентом сумма не используется.
        // Цена ведущего допустима, но только явная и с причиной — она попадает в историю.
        if (req.PriceOverride is not null && string.IsNullOrWhiteSpace(req.OverrideReason))
            throw new DomainRuleException(
                "Для цены, назначенной вручную, нужна причина.", "trade.override_reason_required");
        if (req.PriceOverride is < 0)
            throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");
        // Торг и договорная цена — разные способы, и вместе они бессмысленны: доля считалась бы
        // от цены, которая сама назначена вручную.
        if (req.PriceOverride is not null && req.PricePercent is not null)
            throw new DomainRuleException(
                "Задайте один способ: долю цены или договорную цену.", "trade.purchase_mode_ambiguous");

        // Качество изготовления экземпляра (ROT-WPN-02): у уникальных записей его задаёт каталог,
        // иначе — выбор игрока. Дальше оно неизменно, поэтому проверяется здесь и только здесь.
        var craftsmanship = CraftsmanshipRules.FixedFor(itemDef.Code) ?? req.Craftsmanship;
        CraftsmanshipRules.EnsureApplicable(itemDef.Kind, craftsmanship);

        // Материал магического инструмента (ROT-MAG-MAT-01) — тоже выбор на всю жизнь экземпляра,
        // и он тоже меняет цену, поэтому проверяется до списания.
        ImplementRules.EnsureApplicable(itemDef.Code, req.Material);

        // Больше одной брони и больше двух рук не бывает (ROT-EQP-01): предмет, добавленный сразу
        // «в руки», проверяется теми же правилами, что и надевание уже купленного — и до списания.
        if (req.State == ItemState.Equipped)
            EquipmentSlotRules.EnsureCanEquip(
                itemDef.Kind, itemDef.FormTraits, CharacterDerived.EquippedInputs(c),
                ImplementRules.IsImplement(itemDef.Code) || RuneboundShardRules.IsShard(itemDef.Code));

        var basePrice = itemDef.Price ?? 0;
        var listedUnitPrice = ImplementRules.IsImplement(itemDef.Code)
            ? ImplementRules.Price(basePrice, req.Material)
            : CraftsmanshipRules.Price(basePrice, craftsmanship);
        var unitPrice = req.PriceOverride ?? listedUnitPrice;
        var percent = req.PricePercent ?? 100;
        var total = req.Free
            ? 0
            : req.PriceOverride is not null
                ? TradeRules.PurchaseTotal(unitPrice, req.Quantity)
                : TradeRules.PurchaseTotal(listedUnitPrice, req.Quantity, percent);

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
            ImplementMaterial = req.Material,
            Provenance = req.Free
                ? ItemProvenance.Imported
                : charge.FromBudget > 0 ? ItemProvenance.StartingBudget : ItemProvenance.Purchased,
        };
        db.CharacterItems.Add(item);
        c.Items.Add(item);
        // Надетая броня и есть активная: носят ровно одну, поэтому выбирать не из чего (ROT-CMB-02).
        if (item.State == ItemState.Equipped && itemDef.Kind == ItemKind.Armor)
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
                catalogUnitPrice = itemDef.Price, listedUnitPrice, unitPrice,
                priceOverride = req.PriceOverride, overrideReason = req.OverrideReason,
                free = req.Free, craftsmanship = craftsmanship.ToString(),
                material = req.Material.ToString(),
                percent, mode = req.PriceOverride is not null ? "override"
                    : req.PricePercent is not null ? "haggle" : "direct",
            });

        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}
