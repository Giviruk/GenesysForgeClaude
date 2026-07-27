using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-WPN-01: урон считается из типа и значения профиля, оружие само задаёт свои ограничения
/// дистанции, а щит прибавляет защиту, а не конкурирует с бронёй за максимум.
/// </summary>
public class WeaponProfileRulesTests
{
    private static WeaponAttackProfile Profile(
        DamageKind kind = DamageKind.BrawnPlus, int value = 3, WeaponRange range = WeaponRange.Engaged,
        bool cannotEngage = false) =>
        new() { DamageKind = kind, DamageValue = value, Range = range, CannotAttackEngaged = cannotEngage };

    [Fact]
    public void MeleeDamage_IsBrawnPlusValue_AndRangedDamageIsTheValueItself()
    {
        Assert.Equal(6, WeaponProfileRules.BaseDamage(Profile(DamageKind.BrawnPlus, 3), brawn: 3));
        Assert.Equal(7, WeaponProfileRules.BaseDamage(Profile(DamageKind.Fixed, 7), brawn: 3));
    }

    [Fact]
    public void Pike_DoesNotReachEngaged_ButReachesShort()
    {
        var pike = Profile(range: WeaponRange.Short, cannotEngage: true);

        Assert.False(WeaponProfileRules.CanAttackAt(pike, WeaponRange.Engaged));
        Assert.True(WeaponProfileRules.CanAttackAt(pike, WeaponRange.Short));

        var error = Assert.Throws<DomainRuleException>(
            () => WeaponProfileRules.EnsureCanAttackAt(pike, WeaponRange.Engaged));
        Assert.Equal("weapon.profile.engaged_not_allowed", error.ReasonCode);
    }

    [Fact]
    public void AttackBeyondTheProfileRange_IsRejectedWithItsOwnReasonCode()
    {
        var bow = Profile(DamageKind.Fixed, 7, WeaponRange.Medium);

        var error = Assert.Throws<DomainRuleException>(
            () => WeaponProfileRules.EnsureCanAttackAt(bow, WeaponRange.Long));
        Assert.Equal("weapon.profile.out_of_range", error.ReasonCode);
    }

    [Fact]
    public void OrdinaryWeapon_ReachesEveryBandUpToItsOwn()
    {
        var sword = Profile();

        Assert.True(WeaponProfileRules.CanAttackAt(sword, WeaponRange.Engaged));
        Assert.False(WeaponProfileRules.CanAttackAt(sword, WeaponRange.Short));
    }

    [Fact]
    public void ThrownProfile_ConsumesTheInstance_OnlyWhenItHasLimitedAmmo()
    {
        var thrown = Profile();

        Assert.True(WeaponProfileRules.ConsumesInstance(thrown, ["accurate", "limited-ammo"]));
        Assert.False(WeaponProfileRules.ConsumesInstance(thrown, ["accurate"]));
    }
}
