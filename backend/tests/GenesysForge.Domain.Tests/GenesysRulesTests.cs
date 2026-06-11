using GenesysForge.Domain;

namespace GenesysForge.Domain.Tests;

public class DicePoolTests
{
    [Theory]
    [InlineData(2, 0, 2, 0)] // нет рангов: только зелёные по характеристике
    [InlineData(2, 1, 1, 1)] // 1 ранг: один куб улучшен
    [InlineData(2, 2, 0, 2)] // ранги = характеристике: все жёлтые
    [InlineData(2, 4, 2, 2)] // рангов больше: пул растёт от рангов
    [InlineData(4, 1, 3, 1)]
    [InlineData(0, 3, 3, 0)]
    public void BuildDicePool_FollowsGenesysRule(int characteristic, int ranks, int expectedAbility, int expectedProficiency)
    {
        var pool = GenesysRules.BuildDicePool(characteristic, ranks);
        Assert.Equal(expectedAbility, pool.Ability);
        Assert.Equal(expectedProficiency, pool.Proficiency);
    }

    [Fact]
    public void BuildDicePool_NegativeInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GenesysRules.BuildDicePool(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GenesysRules.BuildDicePool(2, -1));
    }
}

public class CostTests
{
    [Theory]
    [InlineData(1, true, 5)]
    [InlineData(1, false, 10)]
    [InlineData(3, true, 15)]
    [InlineData(5, false, 30)]
    public void SkillRankCost(int newRank, bool career, int expected) =>
        Assert.Equal(expected, GenesysRules.SkillRankCost(newRank, career));

    [Theory]
    [InlineData(1, 5)]
    [InlineData(5, 25)]
    public void TalentCost(int tier, int expected) =>
        Assert.Equal(expected, GenesysRules.TalentCost(tier));

    [Theory]
    [InlineData(3, 30)]
    [InlineData(5, 50)]
    public void CharacteristicUpgradeCost(int newValue, int expected) =>
        Assert.Equal(expected, GenesysRules.CharacteristicUpgradeCost(newValue));
}

public class TalentPyramidTests
{
    private static Dictionary<int, int> Tiers(params (int Tier, int Count)[] counts) =>
        counts.ToDictionary(c => c.Tier, c => c.Count);

    [Fact]
    public void FirstTier1Talent_Allowed() =>
        Assert.True(GenesysRules.CanPurchaseTalentTier(Tiers(), 1));

    [Fact]
    public void Tier2WithoutTier1_Rejected() =>
        Assert.False(GenesysRules.CanPurchaseTalentTier(Tiers(), 2));

    [Fact]
    public void Tier2WithOneTier1_Rejected() =>
        // после покупки будет 1×T1 и 1×T2 — нужно строго больше T1, чем T2
        Assert.False(GenesysRules.CanPurchaseTalentTier(Tiers((1, 1)), 2));

    [Fact]
    public void Tier2WithTwoTier1_Allowed() =>
        Assert.True(GenesysRules.CanPurchaseTalentTier(Tiers((1, 2)), 2));

    [Fact]
    public void SecondTier2_NeedsThreeTier1()
    {
        Assert.False(GenesysRules.CanPurchaseTalentTier(Tiers((1, 2), (2, 1)), 2));
        Assert.True(GenesysRules.CanPurchaseTalentTier(Tiers((1, 3), (2, 1)), 2));
    }

    [Fact]
    public void Tier5_RequiresFullPyramid()
    {
        var pyramid = Tiers((1, 5), (2, 4), (3, 3), (4, 2));
        Assert.True(GenesysRules.CanPurchaseTalentTier(pyramid, 5));

        var broken = Tiers((1, 5), (2, 4), (3, 2), (4, 2));
        Assert.False(GenesysRules.CanPurchaseTalentTier(broken, 5));
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(1, 0)]
    public void InvalidTier_Rejected(int _, int tier) =>
        Assert.False(GenesysRules.CanPurchaseTalentTier(new Dictionary<int, int>(), tier));

    [Theory]
    [InlineData(1, 0, 1)] // первый ранг — базовый тир
    [InlineData(1, 1, 2)] // второй ранг — тир выше
    [InlineData(2, 3, 5)]
    [InlineData(4, 3, 5)] // не выше 5
    public void RankedTalentEffectiveTier(int baseTier, int owned, int expected) =>
        Assert.Equal(expected, GenesysRules.RankedTalentEffectiveTier(baseTier, owned));
}

public class DerivedStatsTests
{
    private static readonly CharacteristicsSet Ch = new(Brawn: 3, Agility: 2, Intellect: 2, Cunning: 2, Willpower: 2, Presence: 2);

    [Fact]
    public void Thresholds_UseArchetypeBaseAndCharacteristics()
    {
        var d = SheetCalculator.ComputeDerived(Ch, archetypeWoundBase: 10, archetypeStrainBase: 10, [], []);
        Assert.Equal(13, d.WoundThreshold);  // 10 + Brawn 3
        Assert.Equal(12, d.StrainThreshold); // 10 + Willpower 2
        Assert.Equal(3, d.Soak);             // Brawn
        Assert.Equal(8, d.EncumbranceThreshold); // 5 + Brawn
    }

    [Fact]
    public void EquippedArmor_AddsSoakAndDefense_AndReducesOwnEncumbrance()
    {
        var armor = new ItemInput("Кольчуга", ItemKind.Armor, ItemState.Equipped,
            Encumbrance: 4, SoakBonus: 2, MeleeDefense: 1, RangedDefense: 1);
        var d = SheetCalculator.ComputeDerived(Ch, 10, 10, [], [armor]);

        Assert.Equal(5, d.Soak);          // 3 + 2
        Assert.Equal(1, d.MeleeDefense);
        Assert.Equal(1, d.RangedDefense);
        Assert.Equal(1, d.EncumbranceLoad); // 4 − 3 (надета)
    }

    [Fact]
    public void UnequippedArmor_GivesNoBonuses_AndFullEncumbrance()
    {
        var armor = new ItemInput("Кольчуга", ItemKind.Armor, ItemState.Backpack,
            Encumbrance: 4, SoakBonus: 2, MeleeDefense: 1);
        var d = SheetCalculator.ComputeDerived(Ch, 10, 10, [], [armor]);

        Assert.Equal(3, d.Soak);
        Assert.Equal(0, d.MeleeDefense);
        Assert.Equal(4, d.EncumbranceLoad);
    }

    [Fact]
    public void Defense_DoesNotStack_TakesBest()
    {
        var shield = new ItemInput("Щит", ItemKind.Weapon, ItemState.Equipped, 2, MeleeDefense: 1);
        var armor = new ItemInput("Латы", ItemKind.Armor, ItemState.Equipped, 5, SoakBonus: 2, MeleeDefense: 2);
        var d = SheetCalculator.ComputeDerived(Ch, 10, 10, [], [shield, armor]);

        Assert.Equal(2, d.MeleeDefense); // max(1, 2), не 3
    }

    [Fact]
    public void EquippedBackpack_RaisesEncumbranceThreshold()
    {
        var backpack = new ItemInput("Рюкзак", ItemKind.Gear, ItemState.Equipped, 0, EncumbranceThresholdBonus: 4);
        var d = SheetCalculator.ComputeDerived(Ch, 10, 10, [], [backpack]);
        Assert.Equal(12, d.EncumbranceThreshold); // 5 + 3 + 4
    }

    [Fact]
    public void Talents_ApplyPassiveBonusesPerRank()
    {
        var toughened = new TalentInput("Toughened", Tier: 1, Ranks: 2, WoundBonusPerRank: 2);
        var grit = new TalentInput("Grit", Tier: 1, Ranks: 1, StrainBonusPerRank: 1);
        var enduring = new TalentInput("Enduring", Tier: 4, Ranks: 1, SoakBonusPerRank: 1);
        var d = SheetCalculator.ComputeDerived(Ch, 10, 10, [toughened, grit, enduring], []);

        Assert.Equal(17, d.WoundThreshold);  // 10 + 3 + 4
        Assert.Equal(13, d.StrainThreshold); // 10 + 2 + 1
        Assert.Equal(4, d.Soak);             // 3 + 1
    }

    [Fact]
    public void Encumbered_WhenLoadExceedsThreshold()
    {
        var loot = new ItemInput("Слитки", ItemKind.Gear, ItemState.Backpack, Encumbrance: 3, Quantity: 4);
        var d = SheetCalculator.ComputeDerived(Ch, 10, 10, [], [loot]);
        Assert.Equal(12, d.EncumbranceLoad);
        Assert.True(d.Encumbered); // 12 > 8
    }

    [Fact]
    public void ComputeSkills_BuildsPoolsFromLinkedCharacteristic()
    {
        var skills = SheetCalculator.ComputeSkills(Ch,
        [
            new SkillInput("Athletics", CharacteristicType.Brawn, 1, true),
            new SkillInput("Charm", CharacteristicType.Presence, 0, false),
        ]);

        Assert.Equal(new DicePool(2, 1), skills[0].Pool); // Brawn 3, ранг 1
        Assert.Equal(new DicePool(2, 0), skills[1].Pool); // Presence 2, ранг 0
    }
}
