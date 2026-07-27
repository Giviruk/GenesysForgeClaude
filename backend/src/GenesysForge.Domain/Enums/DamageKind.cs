namespace GenesysForge.Domain;

/// <summary>
/// Как считается урон профиля атаки (ROT-WPN-01). Раньше это выводилось из строки «+3» на клиенте,
/// поэтому каждый экран разбирал её сам и по-своему.
/// </summary>
public enum DamageKind
{
    /// <summary>Урон = Мощь + значение. Ближний бой и метательное оружие.</summary>
    BrawnPlus = 0,

    /// <summary>Урон равен значению. Дальнобойное оружие.</summary>
    Fixed = 1,
}
