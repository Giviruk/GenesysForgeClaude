using GenesysForge.Domain;

namespace GenesysForge.Domain.Tests;

public class PurchaseValidatorTests
{
    [Fact]
    public void BuySkillRank_CareerCheaperThanNonCareer()
    {
        var career = PurchaseValidator.BuySkillRank(0, isCareer: true, availableXp: 100, isCreationPhase: false);
        var nonCareer = PurchaseValidator.BuySkillRank(0, isCareer: false, availableXp: 100, isCreationPhase: false);
        Assert.True(career.Allowed);
        Assert.Equal(5, career.Cost);
        Assert.Equal(10, nonCareer.Cost);
    }

    [Fact]
    public void BuySkillRank_AboveMax_Rejected()
    {
        var r = PurchaseValidator.BuySkillRank(5, true, 100, false);
        Assert.False(r.Allowed);
    }

    [Fact]
    public void BuySkillRank_CreationCap_Two()
    {
        Assert.True(PurchaseValidator.BuySkillRank(1, true, 100, isCreationPhase: true).Allowed);
        Assert.False(PurchaseValidator.BuySkillRank(2, true, 100, isCreationPhase: true).Allowed);
        Assert.True(PurchaseValidator.BuySkillRank(2, true, 100, isCreationPhase: false).Allowed);
    }

    [Fact]
    public void BuySkillRank_NotEnoughXp_Rejected()
    {
        var r = PurchaseValidator.BuySkillRank(2, true, availableXp: 10, isCreationPhase: false);
        Assert.False(r.Allowed); // нужно 15
    }

    [Fact]
    public void BuyCharacteristic_OnlyAtCreation()
    {
        Assert.True(PurchaseValidator.BuyCharacteristic(2, 100, isCreationPhase: true).Allowed);
        Assert.False(PurchaseValidator.BuyCharacteristic(2, 100, isCreationPhase: false).Allowed);
    }

    [Fact]
    public void BuyCharacteristic_CapFive_CostTenPerNewValue()
    {
        var r = PurchaseValidator.BuyCharacteristic(3, 100, true);
        Assert.True(r.Allowed);
        Assert.Equal(40, r.Cost);
        Assert.False(PurchaseValidator.BuyCharacteristic(5, 1000, true).Allowed);
    }

    [Fact]
    public void BuyTalent_FirstTier1()
    {
        var r = PurchaseValidator.BuyTalent(1, 0, false, new Dictionary<int, int>(), 100);
        Assert.True(r.Allowed);
        Assert.Equal(5, r.Cost);
    }

    [Fact]
    public void BuyTalent_PyramidViolation_Rejected()
    {
        var r = PurchaseValidator.BuyTalent(2, 0, false, new Dictionary<int, int> { [1] = 1 }, 100);
        Assert.False(r.Allowed);
        Assert.Contains("пирамиды", r.Error);
    }

    [Fact]
    public void BuyTalent_NonRankedTwice_Rejected()
    {
        var r = PurchaseValidator.BuyTalent(1, ranksOwned: 1, isRanked: false, new Dictionary<int, int> { [1] = 1 }, 100);
        Assert.False(r.Allowed);
    }

    [Fact]
    public void BuyTalent_RankedSecondRank_CostsHigherTier()
    {
        // Grit (T1) уже куплен 1 раз; второй ранг — эффективный тир 2, нужно ≥2 талантов T1.
        var rejected = PurchaseValidator.BuyTalent(1, 1, true, new Dictionary<int, int> { [1] = 1 }, 100);
        Assert.False(rejected.Allowed);

        var allowed = PurchaseValidator.BuyTalent(1, 1, true, new Dictionary<int, int> { [1] = 2 }, 100);
        Assert.True(allowed.Allowed);
        Assert.Equal(10, allowed.Cost); // тир 2 × 5
    }

    [Fact]
    public void BuyTalent_NotEnoughXp_Rejected()
    {
        var r = PurchaseValidator.BuyTalent(1, 0, false, new Dictionary<int, int>(), availableXp: 4);
        Assert.False(r.Allowed);
    }
}
