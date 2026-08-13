using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Покупает услугу как расход. В отличие от AddItemHandler эта команда намеренно не создаёт
/// CharacterItem: подтверждением и долговременной записью служит audit entry персонажа.
/// </summary>
public class BuyServiceHandler(IAppDbContext db) : ICommandHandler<BuyServiceCommand, Unit>
{
    public async Task<Unit> Handle(BuyServiceCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var character = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (req.Quantity < 1)
            throw new DomainRuleException(
                "Количество должно быть не меньше 1.", "service.quantity_invalid");

        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, command.UserId, character.System, command.CharacterId, ct: ct);
        var service = await db.ItemDefs.AsNoTracking().FirstOrDefaultAsync(i =>
                i.Id == req.ItemDefId && i.System == character.System
                && (i.OwnerUserId == null
                    || (i.HomebrewPackId == null ? i.OwnerUserId == command.UserId
                        : visiblePackIds.Contains(i.HomebrewPackId.Value))), ct)
            ?? throw new DomainRuleException("Услуга не найдена.", "service.not_found");

        if (!ShopCatalogRules.IsService(service.Code))
            throw new DomainRuleException(
                "Эта запись не является услугой.", "service.definition_required");
        if (!req.Free && (!service.Purchasable || service.Price is null))
            throw new DomainRuleException(
                "У услуги нет обычной цены.", "service.not_purchasable");

        var total = req.Free ? 0 : TradeRules.PurchaseTotal(service.Price!.Value, req.Quantity);
        var charge = StartingWallet.Charge(
                total, character.StartingPurchaseBudget, character.Money, character.IsCreationPhase)
            ?? throw new DomainRuleException(
                $"Недостаточно средств: нужно {total}, доступно "
                + $"{character.StartingPurchaseBudget + character.Money}.",
                "character.funds.insufficient");

        character.StartingPurchaseBudget -= charge.FromBudget;
        character.Money -= charge.FromMoney;

        var quantityNote = req.Quantity > 1 ? $" ×{req.Quantity}" : "";
        var costNote = charge.Total > 0 ? $", −{charge.Total}" : ", без оплаты";
        CharacterAudit.Record(db, character, command.UserId, CharacterAuditAction.ServiceBought,
            $"Получена услуга «{service.Name}»{quantityNote}{costNote}", null,
            new
            {
                service = service.Name,
                code = service.Code,
                quantity = req.Quantity,
                unitPrice = service.Price,
                cost = charge.Total,
                fromBudget = charge.FromBudget,
                fromMoney = charge.FromMoney,
                free = req.Free,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
