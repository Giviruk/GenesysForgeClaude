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

        // Покупка: сначала бюджет стартовых покупок (только в фазе создания), затем кошелёк.
        // Cost == null/≤0 — бесплатное добавление.
        var charge = StartingWallet.Charge(req.Cost ?? 0, c.StartingPurchaseBudget, c.Money, c.IsCreationPhase)
            ?? throw new DomainRuleException(
                $"Недостаточно средств: нужно {req.Cost}, доступно {c.StartingPurchaseBudget + c.Money} "
                + $"(бюджет создания {c.StartingPurchaseBudget}, монеты {c.Money}).",
                "character.funds.insufficient");
        c.StartingPurchaseBudget -= charge.FromBudget;
        c.Money -= charge.FromMoney;

        var item = new CharacterItem
        {
            Id = Guid.NewGuid(), CharacterId = c.Id, ItemDefId = itemDef.Id, ItemDef = itemDef,
            Quantity = req.Quantity, State = req.State,
            Provenance = charge.FromBudget > 0 ? ItemProvenance.StartingBudget : ItemProvenance.Purchased,
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
                item = itemDef.Name, quantity = req.Quantity, cost = req.Cost,
                fromBudget = charge.FromBudget, fromMoney = charge.FromMoney,
            });

        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}
