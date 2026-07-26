using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-CRE-03/04: денежные формулы, бюджет создания и целостность карьерного комплекта.</summary>
public class StartingEquipmentTests
{
    // ---- MoneyFormula ----

    [Theory]
    [InlineData(0, "1d100", "1d100", 1, 100)]
    [InlineData(200, "1d100", "200 + 1d100", 201, 300)]
    [InlineData(50, "", "50", 50, 50)]
    public void MoneyFormula_ParsesAndBoundsCorrectly(int fixedPart, string dice, string text, int min, int max)
    {
        var formula = MoneyFormula.Parse(fixedPart, dice);

        Assert.Equal(text, formula.Describe());
        Assert.Equal(min, formula.Minimum);
        Assert.Equal(max, formula.Maximum);
    }

    [Fact]
    public void MoneyFormula_RollIsDeterministicForAGivenRoller()
    {
        var formula = MoneyFormula.Parse(200, "2d10");
        var rolls = new Queue<int>([7, 3]);

        Assert.Equal(210, formula.Roll(_ => rolls.Dequeue()));
    }

    [Theory]
    [InlineData("d100")]
    [InlineData("1d")]
    [InlineData("много")]
    [InlineData("0d100")]
    [InlineData("1d1")]
    public void MoneyFormula_RejectsMalformedDice_InsteadOfSilentlyReturningZero(string dice)
    {
        Assert.False(MoneyFormula.TryParse(0, dice, out _));
        Assert.Throws<ArgumentException>(() => MoneyFormula.Parse(0, dice));
    }

    [Fact]
    public void MoneyFormula_RejectsOutOfRangeRollFromRoller()
    {
        var formula = MoneyFormula.Parse(0, "1d100");
        Assert.Throws<InvalidOperationException>(() => formula.Roll(_ => 0));
    }

    // ---- StartingWallet ----

    [Fact]
    public void Charge_SpendsBudgetFirst_ThenWallet()
    {
        var charge = StartingWallet.Charge(cost: 120, budget: 100, money: 50, isCreationPhase: true);

        Assert.Equal(100, charge!.Value.FromBudget);
        Assert.Equal(20, charge.Value.FromMoney);
    }

    [Fact]
    public void Charge_AfterCreation_IgnoresBudget()
    {
        var charge = StartingWallet.Charge(cost: 40, budget: 500, money: 40, isCreationPhase: false);

        Assert.Equal(0, charge!.Value.FromBudget);
        Assert.Equal(40, charge.Value.FromMoney);
    }

    [Fact]
    public void Charge_ReturnsNull_WhenTotalIsInsufficient()
    {
        Assert.Null(StartingWallet.Charge(cost: 200, budget: 100, money: 50, isCreationPhase: true));
        Assert.Null(StartingWallet.Charge(cost: 10, budget: 500, money: 0, isCreationPhase: false));
    }

    [Fact]
    public void Refund_DuringCreation_RestoresBudgetBeforeWallet()
    {
        // Из 500 потрачено 120; продажа на 200 возвращает 120 в бюджет и 80 в кошелёк.
        var refund = StartingWallet.Refund(200, budget: 380, StartingEquipmentMode.StandardMoney, isCreationPhase: true);

        Assert.Equal(120, refund.FromBudget);
        Assert.Equal(80, refund.FromMoney);
    }

    [Fact]
    public void Refund_InPackageModeOrAfterCreation_GoesEntirelyToWallet()
    {
        Assert.Equal(90, StartingWallet
            .Refund(90, budget: 0, StartingEquipmentMode.CareerPackage, isCreationPhase: true).FromMoney);
        Assert.Equal(90, StartingWallet
            .Refund(90, budget: 380, StartingEquipmentMode.StandardMoney, isCreationPhase: false).FromMoney);
    }

    // ---- CareerPackageResolver ----

    private static CareerStartingGear Fixed(string code, int qty = 1) =>
        new() { ItemCode = code, ItemNameFallback = code, Quantity = qty };

    private static CareerStartingGear Choice(string code, string group, int option, int qty = 1) =>
        new() { ItemCode = code, ItemNameFallback = code, Quantity = qty, IsChoice = true, ChoiceGroup = group, ChoiceOption = option };

    /// <summary>Комплект Scout после errata: кожаная броня фиксированная, первая группа — лук или копьё.</summary>
    private static List<CareerStartingGear> ScoutPackage() =>
    [
        Fixed("leather"), Fixed("backpack"),
        Choice("bow", "slot-1", 0), Choice("spear-light", "slot-1", 1),
        Choice("dagger", "slot-2", 0), Choice("health-elixir", "slot-2", 1, qty: 2),
    ];

    [Theory]
    [InlineData(0, "bow")]
    [InlineData(1, "spear-light")]
    public void Resolve_ScoutEitherBranch_GivesExactlyOneLeather(int option, string expectedWeapon)
    {
        var (lines, error) = CareerPackageResolver.Resolve(
            ScoutPackage(),
            new Dictionary<string, int> { ["slot-1"] = option, ["slot-2"] = 0 },
            []);

        Assert.Null(error);
        Assert.NotNull(lines);
        Assert.Equal(1, lines.Single(l => l.ItemCode == "leather").Quantity);
        Assert.Contains(lines, l => l.ItemCode == expectedWeapon);
    }

    [Fact]
    public void Resolve_MissingGroup_FailsWithReasonCode()
    {
        var (lines, error) = CareerPackageResolver.Resolve(
            ScoutPackage(), new Dictionary<string, int> { ["slot-1"] = 0 }, []);

        Assert.Null(lines);
        Assert.Equal(CareerPackageResolver.ReasonMissingGroup, error!.ReasonCode);
    }

    [Fact]
    public void Resolve_UnknownGroupOrOption_FailsWithReasonCode()
    {
        var unknownGroup = CareerPackageResolver.Resolve(
            ScoutPackage(),
            new Dictionary<string, int> { ["slot-1"] = 0, ["slot-2"] = 0, ["slot-9"] = 0 }, []);
        Assert.Equal(CareerPackageResolver.ReasonUnknownGroup, unknownGroup.Error!.ReasonCode);

        var unknownOption = CareerPackageResolver.Resolve(
            ScoutPackage(),
            new Dictionary<string, int> { ["slot-1"] = 7, ["slot-2"] = 0 }, []);
        Assert.Equal(CareerPackageResolver.ReasonUnknownOption, unknownOption.Error!.ReasonCode);
    }

    [Fact]
    public void Resolve_DuplicateGroup_FailsWithReasonCode()
    {
        var (_, error) = CareerPackageResolver.Resolve(
            ScoutPackage(),
            new Dictionary<string, int> { ["slot-1"] = 0, ["slot-2"] = 0 },
            ["slot-1"]);

        Assert.Equal(CareerPackageResolver.ReasonDuplicateGroup, error!.ReasonCode);
    }

    [Fact]
    public void Resolve_CareerWithoutPackage_FailsWithReasonCode()
    {
        var (_, error) = CareerPackageResolver.Resolve([], new Dictionary<string, int>(), []);

        Assert.Equal(CareerPackageResolver.ReasonNoPackage, error!.ReasonCode);
    }

    [Fact]
    public void Resolve_MergesRepeatedItemCodesIntoOneLine()
    {
        // Scoundrel: фиксированный кинжал и опция «меч + кинжал» дают одну позицию ×2.
        var gear = new List<CareerStartingGear>
        {
            Fixed("dagger"),
            Choice("sword", "slot-1", 0), Choice("dagger", "slot-1", 0),
            Choice("bow", "slot-1", 1),
        };

        var (lines, error) = CareerPackageResolver.Resolve(
            gear, new Dictionary<string, int> { ["slot-1"] = 0 }, []);

        Assert.Null(error);
        Assert.NotNull(lines);
        Assert.Equal(2, lines.Single(l => l.ItemCode == "dagger").Quantity);
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void Slots_AreListedInStableOrder()
    {
        var slots = CareerPackageResolver.Slots(ScoutPackage());

        Assert.Equal(["slot-1", "slot-2"], slots.Select(s => s.ChoiceGroup));
        Assert.Equal([0, 1], slots[0].OptionIndexes);
    }
}
