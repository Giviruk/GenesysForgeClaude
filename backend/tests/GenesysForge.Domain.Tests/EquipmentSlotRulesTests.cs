using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-EQP-01: одновременно носят одну броню и держат две руки. Двуручное оружие занимает обе,
/// одноручное, щит и рукопашное — одну.
/// </summary>
public class EquipmentSlotRulesTests
{
    private static EquippedItemInput Item(
        ItemKind kind, WeaponFormTraits traits = WeaponFormTraits.None, string name = "предмет") =>
        new(Guid.NewGuid(), kind, traits, name);

    private static EquippedItemInput Armor() => Item(ItemKind.Armor, name: "броня");
    private static EquippedItemInput OneHanded() => Item(ItemKind.Weapon, WeaponFormTraits.OneHanded, "меч");
    private static EquippedItemInput TwoHanded() => Item(ItemKind.Weapon, WeaponFormTraits.TwoHanded, "двуручный");

    [Theory]
    [InlineData(WeaponFormTraits.TwoHanded, 2)]
    [InlineData(WeaponFormTraits.OneHanded, 1)]
    [InlineData(WeaponFormTraits.Brawl, 1)]
    // Лук помечен двуручным: держат его обеими руками, как и любое другое двуручное.
    [InlineData(WeaponFormTraits.Ranged | WeaponFormTraits.BowOrCrossbow | WeaponFormTraits.TwoHanded, 2)]
    [InlineData(WeaponFormTraits.Ranged | WeaponFormTraits.OneHanded, 1)]
    public void HandCost_CountsTwoHandedAsBothHands(WeaponFormTraits traits, int expected) =>
        Assert.Equal(expected, EquipmentSlotRules.HandCost(traits));

    [Fact]
    public void SecondArmor_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            EquipmentSlotRules.EnsureCanEquip(ItemKind.Armor, WeaponFormTraits.None, [Armor()]));
        Assert.Equal("equipment.armor_limit", ex.ReasonCode);
    }

    [Fact]
    public void FirstArmor_IsAllowed_EvenWithFullHands() =>
        EquipmentSlotRules.EnsureCanEquip(ItemKind.Armor, WeaponFormTraits.None, [TwoHanded()]);

    [Fact]
    public void TwoLightWeapons_Fit_ButThirdDoesNot()
    {
        EquipmentSlotRules.EnsureCanEquip(ItemKind.Weapon, WeaponFormTraits.OneHanded, [OneHanded()]);

        var ex = Assert.Throws<DomainRuleException>(() => EquipmentSlotRules.EnsureCanEquip(
            ItemKind.Weapon, WeaponFormTraits.OneHanded, [OneHanded(), OneHanded()]));
        Assert.Equal("equipment.hands_full", ex.ReasonCode);
    }

    [Fact]
    public void SecondTwoHandedWeapon_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => EquipmentSlotRules.EnsureCanEquip(
            ItemKind.Weapon, WeaponFormTraits.TwoHanded, [TwoHanded()]));
        Assert.Equal("equipment.hands_full", ex.ReasonCode);
    }

    [Fact]
    public void TwoHandedWeapon_LeavesNoRoomForALightOne()
    {
        // И наоборот: одна занятая рука не позволяет взять двуручное.
        Assert.Throws<DomainRuleException>(() => EquipmentSlotRules.EnsureCanEquip(
            ItemKind.Weapon, WeaponFormTraits.OneHanded, [TwoHanded()]));
        Assert.Throws<DomainRuleException>(() => EquipmentSlotRules.EnsureCanEquip(
            ItemKind.Weapon, WeaponFormTraits.TwoHanded, [OneHanded()]));
    }

    [Fact]
    public void ArmorAndWeapons_DoNotShareLimits()
    {
        // Надетая броня рук не занимает: с ней спокойно берут двуручное.
        EquipmentSlotRules.EnsureCanEquip(ItemKind.Weapon, WeaponFormTraits.TwoHanded, [Armor()]);
        // А снаряжение не ограничено вовсе.
        EquipmentSlotRules.EnsureCanEquip(
            ItemKind.Gear, WeaponFormTraits.None, [Armor(), TwoHanded()]);
    }

    [Fact]
    public void IsValid_ChecksTheWholeSet()
    {
        Assert.True(EquipmentSlotRules.IsValid([Armor(), OneHanded(), OneHanded()]));
        Assert.True(EquipmentSlotRules.IsValid([Armor(), TwoHanded()]));
        Assert.False(EquipmentSlotRules.IsValid([Armor(), Armor()]));
        Assert.False(EquipmentSlotRules.IsValid([TwoHanded(), OneHanded()]));
        Assert.False(EquipmentSlotRules.IsValid([OneHanded(), OneHanded(), OneHanded()]));
    }
}
