namespace GenesysForge.Domain.Tests;

/// <summary>ROT-CRE-02: пороги ран/стрейна замораживаются на момент завершения создания.</summary>
public class ThresholdSnapshotTests
{
    private static CharacteristicsSet Ch(int brawn = 3, int willpower = 3) =>
        new(brawn, 2, 2, 2, willpower, 2);

    private static TalentInput Toughened(int ranks) =>
        new("Toughened", 1, ranks, WoundBonusPerRank: 2, 0, 0, 0, 0);

    private static TalentInput Grit(int ranks) =>
        new("Grit", 1, ranks, 0, StrainBonusPerRank: 1, 0, 0, 0);

    [Fact]
    public void WithoutSnapshot_ThresholdsFollowCurrentCharacteristics()
    {
        var d = SheetCalculator.ComputeDerived(Ch(brawn: 4, willpower: 5), 10, 10, [], []);

        Assert.Equal(14, d.WoundThreshold);
        Assert.Equal(15, d.StrainThreshold);
    }

    [Fact]
    public void WithSnapshot_LaterCharacteristicGain_DoesNotMoveThresholds()
    {
        // Создание завершено при Brawn 3 / Willpower 3, затем Dedication поднял обе до 4.
        var d = SheetCalculator.ComputeDerived(
            Ch(brawn: 4, willpower: 4), 10, 10, [], [],
            woundThresholdSnapshot: 13, strainThresholdSnapshot: 13);

        Assert.Equal(13, d.WoundThreshold);
        Assert.Equal(13, d.StrainThreshold);
    }

    [Fact]
    public void WithSnapshot_BrawnStillDrivesSoakAndEncumbrance()
    {
        var d = SheetCalculator.ComputeDerived(
            Ch(brawn: 4), 10, 10, [], [],
            woundThresholdSnapshot: 13, strainThresholdSnapshot: 13);

        Assert.Equal(4, d.Soak);
        Assert.Equal(9, d.EncumbranceThreshold);
    }

    [Fact]
    public void ExplicitThresholdTalents_AddOnTopOfSnapshotExactlyOnce()
    {
        var d = SheetCalculator.ComputeDerived(
            Ch(brawn: 5, willpower: 5), 10, 10, [Toughened(2), Grit(3)], [],
            woundThresholdSnapshot: 13, strainThresholdSnapshot: 13);

        Assert.Equal(13 + 4, d.WoundThreshold);
        Assert.Equal(13 + 3, d.StrainThreshold);
    }

    [Fact]
    public void SnapshotOnlyForWounds_LeavesStrainDynamic()
    {
        var d = SheetCalculator.ComputeDerived(
            Ch(brawn: 4, willpower: 4), 10, 10, [], [], woundThresholdSnapshot: 13);

        Assert.Equal(13, d.WoundThreshold);
        Assert.Equal(14, d.StrainThreshold);
    }
}
