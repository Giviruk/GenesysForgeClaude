using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-ECO-01: поиск товара по редкости, цена покупки и выручка от продажи.</summary>
public class TradeRulesTests
{
    // ---- таблица редкости ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    [InlineData(7, 4)]
    [InlineData(8, 4)]
    [InlineData(9, 5)]
    [InlineData(10, 5)]
    public void SearchDifficulty_FollowsTheBookTable(int rarity, int expected)
    {
        Assert.Equal(expected, TradeRules.SearchCheck(rarity).Difficulty);
    }

    [Theory]
    [InlineData(11, 1)]
    [InlineData(13, 3)]
    public void RarityAboveTen_KeepsFormidable_AndGrantsUpgrades(int rarity, int expectedUpgrades)
    {
        var check = TradeRules.SearchCheck(rarity);

        Assert.Equal(TradeRules.MaxDifficulty, check.Difficulty);
        Assert.Equal(expectedUpgrades, check.Upgrades);
    }

    [Theory]
    [InlineData(MarketCondition.ConsumerEconomy, -1)]
    [InlineData(MarketCondition.MajorMetropolis, -1)]
    [InlineData(MarketCondition.TradingHub, -1)]
    [InlineData(MarketCondition.Ordinary, 0)]
    [InlineData(MarketCondition.Rural, 1)]
    [InlineData(MarketCondition.RegulatedForThisGood, 1)]
    [InlineData(MarketCondition.Frontier, 2)]
    [InlineData(MarketCondition.ProhibitedOwnership, 2)]
    [InlineData(MarketCondition.ActiveWarZone, 3)]
    [InlineData(MarketCondition.PostDisasterWasteland, 4)]
    public void EveryMarketConditionShiftsRarityByItsOwnAmount(MarketCondition condition, int expected)
    {
        Assert.Equal(expected, TradeRules.RarityModifier(condition));
    }

    [Fact]
    public void ConditionsStack_ButRarityNeverGoesBelowZero()
    {
        Assert.Equal(6, TradeRules.EffectiveRarity(3, [MarketCondition.Frontier, MarketCondition.Rural]));
        Assert.Equal(0, TradeRules.EffectiveRarity(1,
            [MarketCondition.ConsumerEconomy, MarketCondition.TradingHub, MarketCondition.MajorMetropolis]));
    }

    // ---- выручка ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-2, 0)]
    [InlineData(1, 25)]
    [InlineData(2, 50)]
    [InlineData(3, 75)]
    [InlineData(7, 75)]
    public void ProceedsFraction_FollowsTheNumberOfSuccesses(int successes, int expected)
    {
        Assert.Equal(expected, TradeRules.ProceedsPercent(successes));
    }

    [Fact]
    public void Subtotal_TakesTheFractionPerUnit_AndRoundsDown()
    {
        // 25 % от 10 — это 2 за штуку (2,5 вниз) и 10 за пять штук.
        Assert.Equal(10, TradeRules.BookSubtotal(unitListedPrice: 10, quantity: 5, percent: 25));
        // Если бы долю брали от общей цены (25 % от 50 = 12), вышло бы больше — так считать нельзя.
        Assert.NotEqual(12, TradeRules.BookSubtotal(unitListedPrice: 10, quantity: 5, percent: 25));
    }

    [Fact]
    public void Subtotal_ScalesWithQuantity()
    {
        Assert.Equal(150, TradeRules.BookSubtotal(unitListedPrice: 100, quantity: 2, percent: 75));
    }

    [Fact]
    public void ConditionMultiplier_IsExplicit_AndNeverAutomatic()
    {
        var subtotal = TradeRules.BookSubtotal(100, 1, 50);

        // Без множителя цена не режется сама.
        Assert.Equal(50, TradeRules.FinalProceeds(subtotal));
        Assert.Equal(25, TradeRules.FinalProceeds(subtotal, 0.5));
        Assert.Equal(0, TradeRules.FinalProceeds(subtotal, 0));
    }

    [Fact]
    public void NegativeConditionMultiplier_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => TradeRules.FinalProceeds(100, -1));

        Assert.Equal("trade.condition_multiplier_invalid", ex.ReasonCode);
    }

    // ---- покупка ----

    [Fact]
    public void PurchaseTotal_IsListedPriceTimesQuantity()
    {
        Assert.Equal(300, TradeRules.PurchaseTotal(100, 3));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(10, 0)]
    public void InvalidPurchaseInput_IsRejected(int price, int quantity)
    {
        Assert.Throws<DomainRuleException>(() => TradeRules.PurchaseTotal(price, quantity));
    }
}
