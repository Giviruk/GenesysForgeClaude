using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-ARM-01 + ROT-EQP-01: помехи, которые персонаж несёт на себе постоянно, сводятся к каждой
/// конкретной проверке. До этого штрафа брони не существовало, а перегруз был виден только
/// в блоке веса и в бросок не попадал.
/// </summary>
public class CheckModifierAggregatorTests
{
    private static ItemCheckModifierInput Armor(string skill, int value, string condition = "") =>
        new("Plate", "Латы", CheckModifierKind.AddSetback, skill, null, value, condition);

    private static EncumbranceState Overloaded(int overload) =>
        EncumbranceRules.Compute(brawn: 3, load: 8 + overload, thresholdBonuses: 0);

    [Fact]
    public void ArmorPenalty_AppliesOnlyToItsOwnSkill()
    {
        var modifiers = new[] { Armor("Stealth", 2) };

        var stealth = CheckModifierAggregator.For("Stealth", CharacteristicType.Agility, modifiers);
        var athletics = CheckModifierAggregator.For("Athletics", CharacteristicType.Brawn, modifiers);

        Assert.Equal(2, stealth.SetbackDice);
        Assert.Equal(0, athletics.SetbackDice);
    }

    [Fact]
    public void SkillMatch_IsCaseInsensitive_ButNotFuzzy()
    {
        var modifiers = new[] { Armor("stealth", 1) };

        Assert.Equal(1, CheckModifierAggregator.For("Stealth", CharacteristicType.Agility, modifiers).SetbackDice);
        Assert.Equal(0, CheckModifierAggregator.For("Stealth (Group)", CharacteristicType.Agility, modifiers).SetbackDice);
    }

    [Fact]
    public void Overload_AddsSetbackToBrawnAndAgilityChecksOnly()
    {
        // Порог при Мощи 3 — 8; вес 10 даёт превышение 2.
        var enc = Overloaded(2);

        Assert.Equal(2, CheckModifierAggregator.For("Athletics", CharacteristicType.Brawn, null, enc).SetbackDice);
        Assert.Equal(2, CheckModifierAggregator.For("Stealth", CharacteristicType.Agility, null, enc).SetbackDice);
        Assert.Equal(0, CheckModifierAggregator.For("Knowledge", CharacteristicType.Intellect, null, enc).SetbackDice);
    }

    [Fact]
    public void ArmorAndOverload_StackOnTheSameCheck()
    {
        var penalty = CheckModifierAggregator.For(
            "Stealth", CharacteristicType.Agility, [Armor("Stealth", 2)], Overloaded(1));

        Assert.Equal(3, penalty.SetbackDice);
        Assert.Equal(["Item", "Encumbrance"], penalty.Sources.Select(s => s.SourceType));
    }

    [Fact]
    public void RemoveSetback_ReducesTheTotal_ButNeverBelowZero()
    {
        var boots = new ItemCheckModifierInput(
            "Elven Boots", "Эльфийские сапоги", CheckModifierKind.RemoveSetback, "Stealth", null, 3);

        var penalty = CheckModifierAggregator.For(
            "Stealth", CharacteristicType.Agility, [Armor("Stealth", 1), boots]);

        Assert.Equal(0, penalty.SetbackDice);
        // Снятие всё равно видно в разборе: игрок должен понимать, что именно его спасло.
        Assert.Equal(2, penalty.Sources.Count);
    }

    [Fact]
    public void ConditionalModifier_IsShown_ButNotAppliedAutomatically()
    {
        // Приложение не знает, холодная ли сейчас погода, поэтому такой вклад в пул не идёт.
        var clothing = new ItemCheckModifierInput(
            "Winter Clothing", "Зимняя одежда", CheckModifierKind.RemoveSetback, "Survival", null, 2,
            "cold weather");

        var penalty = CheckModifierAggregator.For("Survival", CharacteristicType.Cunning, [clothing]);

        Assert.Equal(0, penalty.SetbackDice);
        var source = Assert.Single(penalty.Sources);
        Assert.True(source.IsConditional);
        Assert.Equal(-2, source.Setback);
    }

    [Fact]
    public void CharacteristicScopedModifier_AppliesToEveryCheckOfThatCharacteristic()
    {
        var shackles = new ItemCheckModifierInput(
            "Shackles", "Кандалы", CheckModifierKind.AddSetback, "", CharacteristicType.Agility, 1);

        Assert.Equal(1, CheckModifierAggregator.For("Coordination", CharacteristicType.Agility, [shackles]).SetbackDice);
        Assert.Equal(0, CheckModifierAggregator.For("Discipline", CharacteristicType.Willpower, [shackles]).SetbackDice);
    }

    [Fact]
    public void NoSources_MeansNoPenaltyAndNoNoise()
    {
        var penalty = CheckModifierAggregator.For("Stealth", CharacteristicType.Agility);

        Assert.Equal(0, penalty.SetbackDice);
        Assert.Empty(penalty.Sources);
    }
}
