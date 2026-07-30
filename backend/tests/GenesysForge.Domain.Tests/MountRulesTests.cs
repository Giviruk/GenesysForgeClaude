using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// Правила скакунов (ROT-MOUNT-ITEM-01): вместимость профиля, перегруз и границы ран.
/// </summary>
public class MountRulesTests
{
    private static MountDef Profile(
        int brawn = 4, int capacity = 18, int woundThreshold = 7, int soak = 0) =>
        new()
        {
            Name = "Beast of Burden", Code = "rot.mount.beast-of-burden",
            Brawn = brawn, Capacity = capacity, WoundThreshold = woundThreshold, Soak = soak,
        };

    [Fact]
    public void ProfileCapacityOverridesFivePlusBrawn()
    {
        var def = Profile(brawn: 4, capacity: 18);

        Assert.Equal(18, MountRules.Capacity(def));
        Assert.Equal(9, MountRules.GenericCapacity(def.Brawn));
    }

    [Fact]
    public void ProfileWithoutOwnCapacityFallsBackToGenericRule()
    {
        // Кастомная запись без числа книги считается общим правилом, а не нулём.
        var def = Profile(brawn: 3, capacity: 0);

        Assert.Equal(8, MountRules.Capacity(def));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(18, false)]
    [InlineData(19, true)]
    public void OverloadStartsStrictlyAboveCapacity(int carriedLoad, bool overloaded)
    {
        Assert.Equal(overloaded, MountRules.IsOverloaded(Profile(), carriedLoad));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(9, true)]
    public void MountIsIncapacitatedOnlyAtOrAboveWoundThreshold(int wounds, bool incapacitated)
    {
        Assert.Equal(incapacitated, MountRules.IsIncapacitated(Profile(woundThreshold: 7), wounds));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(3, 3)]
    [InlineData(40, 7)]
    public void WoundsAreClampedToProfileRange(int input, int expected)
    {
        Assert.Equal(expected, MountRules.ClampWounds(Profile(woundThreshold: 7), input));
    }

    // ── Груз, установленное снаряжение и тяга (ROT-TRANSPORT-01) ──

    private static CharacterItem Cargo(int enc, int quantity = 1, int capacityBonus = 0,
        int soak = 0, int melee = 0, int ranged = 0, bool installed = false) =>
        new()
        {
            Quantity = quantity,
            IsInstalledOnMount = installed,
            ItemDef = new ItemDef
            {
                Name = "gear",
                Encumbrance = enc,
                EncumbranceThresholdBonus = capacityBonus,
                SoakBonus = soak,
                MeleeDefense = melee,
                RangedDefense = ranged,
            },
        };

    [Fact]
    public void CargoLoadCountsWeightTimesQuantityAndSkipsInstalledGear()
    {
        List<CharacterItem> cargo =
            [Cargo(enc: 3, quantity: 4), Cargo(enc: 1), Cargo(enc: 5, installed: true)];

        // 3×4 + 1 = 13; попона весом 5 установлена, а не сложена, и вместимость не занимает.
        Assert.Equal(13, MountRules.CargoLoad(cargo));
    }

    /// <summary>
    /// Правило «десять предметов с нулевым весом дают единицу» — про то, что персонаж таскает на
    /// себе, и на вьючный груз не переносится.
    /// </summary>
    [Fact]
    public void ZeroWeightCargoDoesNotAccumulateLoad()
    {
        Assert.Equal(0, MountRules.CargoLoad([Cargo(enc: 0, quantity: 30)]));
    }

    [Fact]
    public void SaddlebagsRaiseCapacityAndBardingProtectsTheMount()
    {
        List<CharacterItem> cargo =
        [
            Cargo(enc: 0, capacityBonus: 4, installed: true),
            Cargo(enc: 5, soak: 2, melee: 1, installed: true),
        ];

        Assert.Equal(4, MountRules.InstalledCapacityBonus(cargo));
        Assert.Equal(22, MountRules.Capacity(Profile(capacity: 18), MountRules.InstalledCapacityBonus(cargo)));
        Assert.Equal(
            new MountProtection(6, 1, 0),
            MountRules.Protection(Profile(soak: 4), cargo));
        // Перегруз считается от расширенной вместимости, а не от каталожной.
        Assert.False(MountRules.IsOverloaded(Profile(capacity: 18), 22, installedBonus: 4));
        Assert.True(MountRules.IsOverloaded(Profile(capacity: 18), 23, installedBonus: 4));
    }

    /// <summary>
    /// Попона «задаёт Defense 1», а не прибавляет: вьючное животное с нуля поднимается до единицы,
    /// а летающему скакуну с напечатанной дальней защитой 2 она ничего не даёт (ROT-CMB-03).
    /// </summary>
    [Fact]
    public void BardingDefenseCompetesWithTheProfileInsteadOfStacking()
    {
        List<CharacterItem> barding = [Cargo(enc: 5, soak: 2, melee: 1, ranged: 1, installed: true)];

        var burden = MountRules.Protection(Profile(soak: 4), barding);
        Assert.Equal(new MountProtection(6, 1, 1), burden);

        var flyer = Profile(soak: 3);
        flyer.MeleeDefense = 1;
        flyer.RangedDefense = 2;
        var flying = MountRules.Protection(flyer, barding);
        Assert.Equal(new MountProtection(5, 1, 2), flying);
    }

    /// <summary>Груз попону не заменяет: незаустановленная позиция статблок не меняет.</summary>
    [Fact]
    public void CargoThatIsNotInstalledDoesNotProtectTheMount()
    {
        List<CharacterItem> cargo = [Cargo(enc: 5, soak: 2, melee: 1, ranged: 1)];

        Assert.Equal(new MountProtection(4, 0, 0), MountRules.Protection(Profile(soak: 4), cargo));
    }

    [Fact]
    public void BardingNeedsGmApprovalOnAnythingButAWarMount()
    {
        Assert.False(MountRules.RequiresGmApprovalForBarding(
            new MountDef { Name = "War Mount", Code = "rot.mount.war-mount" }));
        Assert.True(MountRules.RequiresGmApprovalForBarding(
            new MountDef { Name = "Beast of Burden", Code = "rot.mount.beast-of-burden" }));
        Assert.True(MountRules.RequiresGmApprovalForBarding(
            new MountDef { Name = "Wagon", Code = "rot.vehicle.wagon" }));
    }

    [Fact]
    public void OnlySelfMovingMountsCanDrawAVehicle()
    {
        var beast = Profile();
        var wagon = new MountDef
        {
            Name = "Wagon",
            TransportKind = TransportKind.Vehicle,
            RequiresTraction = true,
            Capacity = 40,
            WoundThreshold = 10,
        };

        Assert.True(MountRules.CanDraw(beast));
        Assert.False(MountRules.CanDraw(wagon));
        Assert.False(MountRules.NeedsTraction(beast, null));
        Assert.True(MountRules.NeedsTraction(wagon, null));
        Assert.False(MountRules.NeedsTraction(wagon, Guid.NewGuid()));
    }
}
