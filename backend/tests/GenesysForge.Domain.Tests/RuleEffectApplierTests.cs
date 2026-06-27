using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class RuleEffectApplierTests
{
    private static GameParticipant Target() => new()
    {
        DisplayName = "T", WoundsCurrent = 8, WoundsThreshold = 12,
        StrainCurrent = 4, StrainThreshold = 10, Soak = 3, MeleeDefense = 1, RangedDefense = 0,
    };

    private static RuleEffectDef E(RuleEffectKind kind, int amount = 0) => new() { Kind = kind, Amount = amount };

    [Fact]
    public void HealWounds_ReducesCurrent_NotBelowZero()
    {
        var t = Target();
        var r = RuleEffectApplier.Apply([E(RuleEffectKind.HealWounds, 3)], t);
        Assert.Equal(5, t.WoundsCurrent);
        Assert.True(r.AnyApplied);

        RuleEffectApplier.Apply([E(RuleEffectKind.HealWounds, 99)], t);
        Assert.Equal(0, t.WoundsCurrent);
    }

    [Fact]
    public void AdjustSoak_AddsAndClampsAtZero()
    {
        var t = Target();
        RuleEffectApplier.Apply([E(RuleEffectKind.AdjustSoak, 4)], t);
        Assert.Equal(7, t.Soak);
        RuleEffectApplier.Apply([E(RuleEffectKind.AdjustSoak, -99)], t);
        Assert.Equal(0, t.Soak);
    }

    [Fact]
    public void AdjustDefenseAndThresholds_Apply()
    {
        var t = Target();
        RuleEffectApplier.Apply([
            E(RuleEffectKind.AdjustMeleeDefense, 1),
            E(RuleEffectKind.AdjustRangedDefense, 2),
            E(RuleEffectKind.AdjustWoundThreshold, 5),
            E(RuleEffectKind.AdjustStrainThreshold, -2),
        ], t);
        Assert.Equal(2, t.MeleeDefense);
        Assert.Equal(2, t.RangedDefense);
        Assert.Equal(17, t.WoundsThreshold);
        Assert.Equal(8, t.StrainThreshold);
    }

    [Fact]
    public void BoostStoryManual_GoIntoManualPrompts_NoStateChange()
    {
        var t = Target();
        var r = RuleEffectApplier.Apply([
            E(RuleEffectKind.AddBoostNextCheck, 1),
            E(RuleEffectKind.AddSetbackNextCheck, 1),
            E(RuleEffectKind.SpendStoryPoint),
            E(RuleEffectKind.Manual),
        ], t);
        Assert.Empty(r.Applied);
        Assert.Equal(3, r.Manual.Count); // boost, setback, story (Manual без описания пропускается)
        Assert.Equal(8, t.WoundsCurrent); // состояние не изменилось
    }
}
