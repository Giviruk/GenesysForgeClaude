using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-EQP-01: перегруз, его цена и вес предметов с нулевым Enc.</summary>
public class EncumbranceRulesTests
{
    [Fact]
    public void ThresholdIsFivePlusBrawnPlusBonuses()
    {
        Assert.Equal(7, EncumbranceRules.Compute(brawn: 2, load: 0).Threshold);
        Assert.Equal(11, EncumbranceRules.Compute(brawn: 2, load: 0, thresholdBonuses: 4).Threshold);
    }

    [Fact]
    public void WithinTheThreshold_NothingHappens()
    {
        var e = EncumbranceRules.Compute(brawn: 2, load: 7);

        Assert.False(e.Encumbered);
        Assert.Equal(0, e.SetbackDice);
        Assert.True(e.HasFreeManoeuvre);
        Assert.Equal(0, e.StrainPerManoeuvre);
    }

    [Fact]
    public void LightOverload_AddsSetbackButKeepsTheFreeManoeuvre()
    {
        // Пример ТЗ: Мощь 2, порог 7, вес 8 → одна помеха и бесплатный манёвр на месте.
        var e = EncumbranceRules.Compute(brawn: 2, load: 8);

        Assert.Equal(1, e.Overload);
        Assert.Equal(1, e.SetbackDice);
        Assert.True(e.HasFreeManoeuvre);
        Assert.Equal(0, e.StrainPerManoeuvre);
    }

    [Fact]
    public void OverloadEqualToBrawn_CostsTheFreeManoeuvreAndChargesEveryManoeuvre()
    {
        // Тот же пример: вес 9 → две помехи и 2 усталости за каждый манёвр, включая первый.
        var e = EncumbranceRules.Compute(brawn: 2, load: 9);

        Assert.Equal(2, e.Overload);
        Assert.Equal(2, e.SetbackDice);
        Assert.False(e.HasFreeManoeuvre);
        Assert.Equal(2, e.StrainPerManoeuvre);
    }

    [Fact]
    public void SetbackGrowsWithEveryPointOfOverload()
    {
        Assert.Equal(5, EncumbranceRules.Compute(brawn: 3, load: 13).SetbackDice);
    }

    // ---- предметы с нулевым Enc ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 1)]
    [InlineData(19, 1)]
    [InlineData(20, 2)]
    [InlineData(21, 2)]
    public void LooseZeroEncumbranceItems_CountTenToAPoint(int count, int expected)
    {
        Assert.Equal(expected, EncumbranceRules.ZeroEncumbranceLoad(count));
    }

    [Theory]
    [InlineData(19, 0)]
    [InlineData(20, 1)]
    [InlineData(40, 2)]
    public void EfficientlyStoredZeroEncumbranceItems_CountTwentyToAPoint(int count, int expected)
    {
        Assert.Equal(expected, EncumbranceRules.ZeroEncumbranceLoad(0, count));
    }

    [Fact]
    public void ZeroEncumbranceLoad_AddsToTheCarriedWeight()
    {
        var e = EncumbranceRules.Compute(brawn: 2, load: 6, zeroEncLoad: 2);

        Assert.Equal(8, e.Load);
        Assert.Equal(1, e.Overload);
    }

    [Fact]
    public void SheetCalculator_AggregatesZeroEncumbranceAcrossSeparateStacks()
    {
        var ch = new CharacteristicsSet(2, 2, 2, 2, 2, 2);
        // Десять строк по одному предмету не должны обойти правило, как и один стек из десяти.
        var separate = Enumerable.Range(0, 10)
            .Select(i => new ItemInput($"Факел {i}", ItemKind.Gear, ItemState.Backpack, 0))
            .ToList();
        var single = new[] { new ItemInput("Факелы", ItemKind.Gear, ItemState.Backpack, 0, Quantity: 10) };

        var fromSeparate = SheetCalculator.ComputeDerived(ch, 10, 10, [], separate);
        var fromSingle = SheetCalculator.ComputeDerived(ch, 10, 10, [], single);

        Assert.Equal(1, fromSeparate.EncumbranceLoad);
        Assert.Equal(1, fromSingle.EncumbranceLoad);
    }

    [Fact]
    public void SheetCalculator_ReportsTheExactOverloadPenalty()
    {
        var ch = new CharacteristicsSet(2, 2, 2, 2, 2, 2);
        var heavy = new ItemInput("Наковальня", ItemKind.Gear, ItemState.Backpack, 9);

        var d = SheetCalculator.ComputeDerived(ch, 10, 10, [], [heavy]);

        Assert.True(d.Encumbered);
        Assert.Equal(2, d.Encumbrance!.SetbackDice);
        Assert.False(d.Encumbrance.HasFreeManoeuvre);
        Assert.Equal(2, d.Encumbrance.StrainPerManoeuvre);
    }
}
