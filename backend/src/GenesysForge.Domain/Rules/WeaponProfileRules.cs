using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Правила профилей атаки (ROT-WPN-01): урон считается из типа и значения, а не из строки, и
/// оружие может само запрещать дистанцию или задавать сложность.
/// </summary>
public static class WeaponProfileRules
{
    /// <summary>Коды профилей, известных приложению.</summary>
    public const string ThrownCode = "thrown";
    public const string HeldCode = "held";

    /// <summary>
    /// Базовый урон профиля до броска: для ближнего боя и метания — Мощь плюс значение,
    /// для дальнобойного — само значение.
    /// </summary>
    public static int BaseDamage(DamageKind kind, int damageValue, int brawn) => kind switch
    {
        DamageKind.BrawnPlus => Math.Max(0, brawn) + damageValue,
        DamageKind.Fixed => damageValue,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <inheritdoc cref="BaseDamage(DamageKind, int, int)"/>
    public static int BaseDamage(WeaponAttackProfile profile, int brawn) =>
        BaseDamage(profile.DamageKind, profile.DamageValue, brawn);

    /// <summary>
    /// Можно ли атаковать этим профилем на указанной дистанции. Пика не достаёт вплотную, а
    /// дальнобойное оружие не бьёт дальше своей полосы.
    /// </summary>
    public static bool CanAttackAt(WeaponAttackProfile profile, WeaponRange target)
    {
        if (profile.CannotAttackEngaged && target == WeaponRange.Engaged) return false;
        return target <= profile.Range;
    }

    /// <summary>
    /// Проверяет дистанцию и бросает доменную ошибку с машинным кодом — атака вплотную пикой
    /// не должна молча превращаться в обычную.
    /// </summary>
    public static void EnsureCanAttackAt(WeaponAttackProfile profile, WeaponRange target)
    {
        if (profile.CannotAttackEngaged && target == WeaponRange.Engaged)
            throw new DomainRuleException(
                "Этим оружием нельзя атаковать вплотную.", "weapon.profile.engaged_not_allowed");
        if (target > profile.Range)
            throw new DomainRuleException(
                "Цель дальше, чем достаёт это оружие.", "weapon.profile.out_of_range");
    }

    /// <summary>
    /// Метательный профиль расходует сам экземпляр: после броска оружие лежит у цели, пока его
    /// не подберут. Уничтожать его нельзя — это не боеприпас.
    /// </summary>
    public static bool ConsumesInstance(WeaponAttackProfile profile, IEnumerable<string> qualityCodes) =>
        qualityCodes.Any(c => string.Equals(c, "limited-ammo", StringComparison.OrdinalIgnoreCase));
}
