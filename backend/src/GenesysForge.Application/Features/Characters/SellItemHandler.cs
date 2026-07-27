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

        // Способы продажи взаимоисключающие: доля по проверке, выбранная доля цены каталога или
        // договорная цена за штуку. Сумму во всех случаях считает сервер — «сколько начислить»
        // клиент не присылает ни в одном из них.
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

        var listedUnitPrice = item.ItemDef?.Price ?? 0;
        int percent;
        int bookSubtotal;

        if (req.PriceOverride is { } overridePrice)
        {
            // Договорная цена: она и есть цена за штуку, доля к ней не применяется.
            if (string.IsNullOrWhiteSpace(req.OverrideReason))
                throw new DomainRuleException(
                    "Для договорной цены нужна причина.", "trade.override_reason_required");
            if (overridePrice < 0)
                throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");

            percent = 100;
            bookSubtotal = TradeRules.PurchaseTotal(overridePrice, req.Quantity);
        }
        else if (req.NetSuccesses is { } successes)
        {
            percent = TradeRules.ProceedsPercent(successes);
            if (percent == 0)
                throw new DomainRuleException(
                    "Проверка продажи провалена — сделка не состоялась.", "trade.sale_failed");
            bookSubtotal = TradeRules.BookSubtotal(listedUnitPrice, req.Quantity, percent);
        }
        else
        {
            percent = req.Percent ?? 100;
            if (percent is < 0 or > 100)
                throw new DomainRuleException(
                    "Доля цены при продаже задаётся от 0 до 100 процентов.", "trade.percent_invalid");
            bookSubtotal = TradeRules.BookSubtotal(listedUnitPrice, req.Quantity, percent);
        }
        var proceeds = TradeRules.FinalProceeds(bookSubtotal, req.ConditionMultiplier ?? 1.0);

        // Во время создания выручка сначала восстанавливает бюджет: иначе цикл «купить → продать»
        // превращал бы бюджет 500 в реальные деньги.
        var refund = StartingWallet.Refund(proceeds, c.StartingPurchaseBudget, c.StartingEquipmentMode, c.IsCreationPhase);
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
            $"Продан предмет «{itemName}»{qtyNote}, +{proceeds}", null,
            new
            {
                item = itemName, quantity = req.Quantity, proceeds,
                toBudget = refund.FromBudget, toMoney = refund.FromMoney,
                // История хранит всё, из чего сложилась сумма: цену, долю, промежуточный итог и поправку.
                listedUnitPrice, netSuccesses = req.NetSuccesses, percent, bookSubtotal,
                mode = req.PriceOverride is not null ? "override"
                    : req.NetSuccesses is not null ? "check" : "direct",
                priceOverride = req.PriceOverride, overrideReason = req.OverrideReason,
                conditionMultiplier = req.ConditionMultiplier, conditionReason = req.ConditionReason,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
