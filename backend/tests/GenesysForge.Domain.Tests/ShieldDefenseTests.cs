using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-WPN-01: щит — оружие, а не броня. Его Defensive и Deflection — надбавки «+N», поэтому они
/// складываются с бронёй, а не конкурируют с ней за максимум, как было раньше.
/// </summary>
public class ShieldDefenseTests
{
    private static CharacteristicsSet Ch() => new(3, 2, 2, 2, 2, 2);

    private static ItemInput Armor(string name, int defense, bool active = true) =>
        new(name, ItemKind.Armor, ItemState.Equipped, 2, 1, 0, defense, defense, 0, active);

    private static ItemInput Shield(string name, int defensive, int deflection, ItemState state = ItemState.Equipped) =>
        new(name, ItemKind.Weapon, state, 2, 1, Qualities:
            [new ItemQualityInput("defensive", defensive), new ItemQualityInput("deflection", deflection)]);

    private static DerivedStats Compute(params ItemInput[] items) =>
        SheetCalculator.ComputeDerived(Ch(), 10, 10, [], items);

    [Fact]
    public void ShieldAddsToArmor_InsteadOfCompetingWithIt()
    {
        var d = Compute(Armor("Латы", defense: 1), Shield("Большой щит", defensive: 2, deflection: 2));

        // Раньше побеждал максимум и выходило 2; по правилу броня 1 плюс надбавка щита 2.
        Assert.Equal(3, d.MeleeDefense);
        Assert.Equal(3, d.RangedDefense);
    }

    [Fact]
    public void DefensiveWorksInMelee_AndDeflectionAtRange()
    {
        var d = Compute(Shield("Щит", defensive: 1, deflection: 0));

        Assert.Equal(1, d.MeleeDefense);
        Assert.Equal(0, d.RangedDefense);
    }

    [Fact]
    public void TwoShields_StackTheirIncreases_UpToTheCapOfFour()
    {
        var d = Compute(
            Armor("Латы", defense: 1),
            Shield("Большой щит", defensive: 2, deflection: 2),
            Shield("Бульварк", defensive: 2, deflection: 3));

        // 1 + 2 + 2 = 5 упирается в предел 4 (ROT-CMB-03).
        Assert.Equal(4, d.MeleeDefense);
        Assert.True(d.MeleeDefenseBreakdown!.Capped);
    }

    [Fact]
    public void ShieldInTheBackpack_DoesNotDefendAnyone()
    {
        var d = Compute(Shield("Щит", defensive: 1, deflection: 1, state: ItemState.Backpack));

        Assert.Equal(0, d.MeleeDefense);
        Assert.Equal(0, d.RangedDefense);
    }

    [Fact]
    public void ThrownWeapon_GivesNeitherQualitiesNorLoad()
    {
        var inHand = Shield("Щит", defensive: 1, deflection: 1);
        var thrown = inHand with { IsThrown = true };

        Assert.Equal(1, Compute(inHand).MeleeDefense);
        Assert.Equal(0, Compute(thrown).MeleeDefense);
        Assert.Equal(0, SheetCalculator.ItemLoad(thrown));
    }

    [Fact]
    public void CustomItemWithoutStructuralQualities_StillAddsItsColumns()
    {
        // У кастомного снаряжения структурных качеств может не быть: числовые колонки остаются
        // рабочими, но теперь тоже как надбавка, а не как провайдер.
        var talisman = new ItemInput("Оберег", ItemKind.Gear, ItemState.Equipped, 0, 1,
            MeleeDefense: 1, RangedDefense: 1);

        var d = Compute(Armor("Латы", defense: 1), talisman);

        Assert.Equal(2, d.MeleeDefense);
    }
}
