using GenesysForge.Domain;

namespace GenesysForge.Domain.Tests;

public class RefundValidatorTests
{
    [Fact]
    public void RefundCharacteristic_ReturnsCostOfCurrentValue()
    {
        // купили 2→3 за 30 — возвращаем 30
        var r = PurchaseValidator.RefundCharacteristic(currentValue: 3, archetypeBase: 2, isCreationPhase: true);
        Assert.True(r.Allowed);
        Assert.Equal(30, r.Cost);
    }

    [Fact]
    public void RefundCharacteristic_NotBelowArchetypeBase()
    {
        var r = PurchaseValidator.RefundCharacteristic(2, 2, true);
        Assert.False(r.Allowed);
    }

    [Fact]
    public void RefundCharacteristic_OnlyDuringCreation()
    {
        var r = PurchaseValidator.RefundCharacteristic(3, 2, isCreationPhase: false);
        Assert.False(r.Allowed);
    }

    [Theory]
    [InlineData(2, 0, true, 10)]  // карьерный, ранг 2 стоил 10
    [InlineData(1, 0, false, 10)] // некарьерный, ранг 1 стоил 10
    [InlineData(2, 1, true, 10)]  // ранг 2 поверх бесплатного — возвращается
    public void RefundSkillRank_ReturnsCostOfCurrentRank(int rank, int freeRanks, bool career, int expected)
    {
        var r = PurchaseValidator.RefundSkillRank(rank, freeRanks, career, true);
        Assert.True(r.Allowed);
        Assert.Equal(expected, r.Cost);
    }

    [Fact]
    public void RefundSkillRank_FreeRank_Protected()
    {
        var r = PurchaseValidator.RefundSkillRank(currentRank: 1, freeRanks: 1, isCareer: true, isCreationPhase: true);
        Assert.False(r.Allowed);
    }

    [Fact]
    public void RefundSkillRank_ZeroRanks_Rejected()
    {
        Assert.False(PurchaseValidator.RefundSkillRank(0, 0, true, true).Allowed);
    }

    [Fact]
    public void RefundSkillRank_OnlyDuringCreation()
    {
        Assert.False(PurchaseValidator.RefundSkillRank(2, 0, true, isCreationPhase: false).Allowed);
    }

    private static Dictionary<int, int> Tiers(params (int Tier, int Count)[] counts) =>
        counts.ToDictionary(c => c.Tier, c => c.Count);

    [Fact]
    public void CanRemoveTalentTier_SimpleRemoval()
    {
        Assert.True(GenesysRules.CanRemoveTalentTier(Tiers((1, 1)), 1));
        Assert.False(GenesysRules.CanRemoveTalentTier(Tiers((1, 1)), 2)); // нечего убирать
    }

    [Fact]
    public void CanRemoveTalentTier_PyramidProtected()
    {
        // 2×T1 + 1×T2: убрать T1 нельзя (останется 1×T1 при 1×T2), убрать T2 можно
        var pyramid = Tiers((1, 2), (2, 1));
        Assert.False(GenesysRules.CanRemoveTalentTier(pyramid, 1));
        Assert.True(GenesysRules.CanRemoveTalentTier(pyramid, 2));

        // 3×T1 + 1×T2: запас есть — T1 можно убрать
        Assert.True(GenesysRules.CanRemoveTalentTier(Tiers((1, 3), (2, 1)), 1));
    }

    [Fact]
    public void RefundTalent_RankedRefundsLastRankTier()
    {
        // Grit (T1) куплен дважды: ранги заняли тиры 1 и 2; возврат последнего ранга = тир 2 = 10 XP
        var r = PurchaseValidator.RefundTalent(baseTier: 1, ranksOwned: 2, Tiers((1, 2), (2, 1)), true);
        Assert.True(r.Allowed);
        Assert.Equal(10, r.Cost);
    }

    [Fact]
    public void RefundTalent_PyramidViolation_Rejected()
    {
        var r = PurchaseValidator.RefundTalent(1, 1, Tiers((1, 2), (2, 1)), true);
        Assert.False(r.Allowed);
        Assert.Contains("пирамида", r.Error);
    }

    [Fact]
    public void RefundTalent_NotOwned_Rejected()
    {
        Assert.False(PurchaseValidator.RefundTalent(1, 0, Tiers(), true).Allowed);
    }

    [Fact]
    public void RefundTalent_OnlyDuringCreation()
    {
        Assert.False(PurchaseValidator.RefundTalent(1, 1, Tiers((1, 1)), isCreationPhase: false).Allowed);
    }

    [Fact]
    public void BuyThenRefund_IsSymmetric()
    {
        // покупка и возврат той же характеристики дают одинаковую сумму
        var buy = PurchaseValidator.BuyCharacteristic(2, 100, true);
        var refund = PurchaseValidator.RefundCharacteristic(3, 2, true);
        Assert.Equal(buy.Cost, refund.Cost);
    }
}
