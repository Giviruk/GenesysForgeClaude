using GenesysForge.Domain;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-CMB-02: защиту и поглощение даёт ровно одна выбранная броня.</summary>
public class ActiveArmorTests
{
    private static CharacteristicsSet Ch(int brawn = 3) => new(brawn, 2, 2, 2, 2, 2);

    private static ItemInput Armor(string name, int soak, int defense = 0, bool active = false) =>
        new(name, ItemKind.Armor, ItemState.Equipped, 2, 1, soak, defense, defense, 0, active);

    private static DerivedStats Compute(params ItemInput[] items) =>
        SheetCalculator.ComputeDerived(Ch(), 10, 10, [], items);

    [Fact]
    public void TwoWornArmors_DoNotStackTheirSoak()
    {
        var d = Compute(Armor("Кожаная", soak: 1, active: true), Armor("Кольчуга", soak: 2));

        // Мощь 3 + поглощение только активной брони 1, а не 1 + 2.
        Assert.Equal(4, d.Soak);
    }

    [Fact]
    public void SwitchingTheActiveArmor_ChangesProtection_ButNotTheCarriedLoad()
    {
        var before = Compute(Armor("Кожаная", soak: 1, active: true), Armor("Кольчуга", soak: 2));
        var after = Compute(Armor("Кожаная", soak: 1), Armor("Кольчуга", soak: 2, active: true));

        Assert.Equal(4, before.Soak);
        Assert.Equal(5, after.Soak);
        // Обе брони по-прежнему надеты, поэтому переносимый вес не меняется.
        Assert.Equal(before.EncumbranceLoad, after.EncumbranceLoad);
    }

    [Fact]
    public void InactiveArmor_GivesNoDefense()
    {
        var d = Compute(Armor("Кожаная", soak: 1, defense: 0, active: true), Armor("Щитовая", soak: 0, defense: 2));

        Assert.Equal(0, d.MeleeDefense);
        Assert.Equal(0, d.RangedDefense);
    }

    [Fact]
    public void WithoutAnActiveArmor_NoArmorProtectionApplies()
    {
        var d = Compute(Armor("Кожаная", soak: 1), Armor("Кольчуга", soak: 2, defense: 1));

        Assert.Equal(3, d.Soak); // только Мощь
        Assert.Equal(0, d.MeleeDefense);
    }

    [Fact]
    public void NonArmorItems_KeepWorkingRegardlessOfTheArmorChoice()
    {
        // Щит и талисман — не броня, поэтому активность брони их не отключает.
        var shield = new ItemInput("Щит", ItemKind.Gear, ItemState.Equipped, 1, 1, 0, 2, 0, 0);
        var d = Compute(Armor("Кольчуга", soak: 2), shield);

        Assert.Equal(2, d.MeleeDefense);
        Assert.Equal(3, d.Soak); // броня не активна — её поглощение не считается
    }

    [Fact]
    public void OnlyTheActiveArmorGivesItsEncumbranceThresholdBonus()
    {
        var bulky = new ItemInput("Рюкзак-броня", ItemKind.Armor, ItemState.Equipped, 2, 1, 0, 0, 0, 4);
        var active = bulky with { IsActiveArmor = true };

        Assert.Equal(
            SheetCalculator.ComputeDerived(Ch(), 10, 10, [], [active]).EncumbranceThreshold,
            SheetCalculator.ComputeDerived(Ch(), 10, 10, [], [bulky]).EncumbranceThreshold + 4);
    }
}
