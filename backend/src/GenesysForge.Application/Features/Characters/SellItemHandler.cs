using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

public class SellItemHandler(IAppDbContext db) : ICommandHandler<SellItemCommand, Unit>
{
    public async Task<Unit> Handle(SellItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");

        if (req.Quantity < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");
        if (req.Quantity > item.Quantity)
            throw new DomainRuleException($"Нельзя продать больше, чем есть ({item.Quantity}).");
        if (req.Proceeds < 0) throw new DomainRuleException("Выручка не может быть отрицательной.");

        // Во время создания выручка сначала восстанавливает бюджет: иначе цикл «купить → продать»
        // превращал бы бюджет 500 в реальные деньги.
        var refund = StartingWallet.Refund(req.Proceeds, c.StartingPurchaseBudget, c.StartingEquipmentMode, c.IsCreationPhase);
        c.StartingPurchaseBudget += refund.FromBudget;
        c.Money += refund.FromMoney;
        var itemName = item.ItemDef?.Name ?? "предмет";

        if (req.Quantity == item.Quantity)
        {
            // Проданная целиком броня перестаёт быть активной (ROT-CMB-02).
            if (c.ActiveArmorCharacterItemId == item.Id) c.ActiveArmorCharacterItemId = null;
            c.Items.Remove(item);
            db.CharacterItems.Remove(item);
        }
        else
        {
            item.Quantity -= req.Quantity;
        }

        var qtyNote = req.Quantity > 1 ? $" ×{req.Quantity}" : "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemSold,
            $"Продан предмет «{itemName}»{qtyNote}, +{req.Proceeds}", null,
            new
            {
                item = itemName, quantity = req.Quantity, proceeds = req.Proceeds,
                toBudget = refund.FromBudget, toMoney = refund.FromMoney,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
