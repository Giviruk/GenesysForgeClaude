using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class CriticalInjuryRulesTests
{
    private static CharacterCriticalInjury Injury(string code, string name = "Критическая травма") =>
        new() { RuleCode = code, NameRu = name };

    [Fact]
    public void Tinnitus_AddsDifficultyToIntellectAndCunningChecks()
    {
        var modifiers = CriticalInjuryRules.CheckModifiers([Injury("crit-ci_046_050", "Звон в ушах")]);

        var knowledge = CheckModifierAggregator.For(
            "Knowledge (Lore)", CharacteristicType.Intellect, criticalInjuries: modifiers);
        var stealth = CheckModifierAggregator.For(
            "Stealth", CharacteristicType.Agility, criticalInjuries: modifiers);

        Assert.Equal(1, knowledge.DifficultyDice);
        Assert.Equal(0, stealth.DifficultyDice);
    }

    [Fact]
    public void Blindness_UsesThreeUpgradesForPerceptionAndTwoForOtherChecks()
    {
        var modifiers = CriticalInjuryRules.CheckModifiers([Injury("crit-ci_116_120", "Слепота")]);

        var perception = CheckModifierAggregator.For(
            "Perception", CharacteristicType.Intellect, criticalInjuries: modifiers);
        var melee = CheckModifierAggregator.For(
            "Melee", CharacteristicType.Brawn, criticalInjuries: modifiers);

        Assert.Equal(3, perception.DifficultyUpgrades);
        Assert.Equal(2, melee.DifficultyUpgrades);
    }

    [Fact]
    public void ShortLivedNextCheckEffects_AreNotAppliedForever()
    {
        var modifiers = CriticalInjuryRules.CheckModifiers([Injury("crit-ci_021_025", "Потеря равновесия")]);

        var penalty = CheckModifierAggregator.For(
            "Athletics", CharacteristicType.Brawn, criticalInjuries: modifiers);

        Assert.Equal(0, penalty.SetbackDice);
        Assert.Equal(0, penalty.DifficultyDice);
        Assert.Empty(penalty.Sources);
    }

    [Fact]
    public void DisorientedFeeling_RemovesBoostDiceFromTheCheck()
    {
        var modifiers = CriticalInjuryRules.CheckModifiers([Injury("crit-ci_066_070", "В растерянных чувствах")]);
        var penalty = CheckModifierAggregator.For(
            "Stealth", CharacteristicType.Agility,
            skillBoosts: [new AttachmentSkillBoost("Stealth", 2, "Elven cloak", "Эльфийский плащ")],
            criticalInjuries: modifiers);

        Assert.True(penalty.RemoveBoosts);
        Assert.Equal(0, penalty.BoostDice);
        Assert.Contains(penalty.Sources, source => source.RemoveBoosts);
    }
}
