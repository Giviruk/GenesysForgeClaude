namespace GenesysForge.Domain;

/// <summary>
/// Параметр, который primary effect требует выбрать вместе с собой (ROT-HA-02). Определяется
/// стабильным кодом способности, а не её отображаемым именем.
/// </summary>
public enum HeroicParameterKind
{
    /// <summary>Способность не требует параметра.</summary>
    None = 0,

    /// <summary>Paragon: ровно один доступный персонажу навык.</summary>
    ParagonSkill = 1,

    /// <summary>Sixth Sense: одна согласованная с GM категория воспринимаемых существ или явлений.</summary>
    SixthSenseSubject = 2,

    /// <summary>Signature Weapon: профиль, качество изготовления, форма и её признаки.</summary>
    SignatureWeapon = 3,
}

/// <summary>Профиль именного оружия. Числа профиля строит сервер, клиент присылает только выбор.</summary>
public enum SignatureWeaponProfile
{
    Brawl = 0,
    OneHanded = 1,
    TwoHanded = 2,
    Ranged = 3,
}

/// <summary>Качество изготовления именного оружия.</summary>
public enum WeaponCraftsmanship
{
    Dwarven = 0,
    Elven = 1,
    Steel = 2,
}

/// <summary>
/// Неизменяемые признаки формы оружия (ROT-HA-02). Совместимость attachment определяется ими,
/// а не свободным названием формы: по тексту «меч-посох» нельзя вывести, есть ли режущая кромка.
/// </summary>
[Flags]
public enum WeaponFormTraits
{
    None = 0,

    /// <summary>Группа профиля: рукопашная форма.</summary>
    Brawl = 1 << 0,

    /// <summary>Группа профиля: одноручная ближняя форма.</summary>
    OneHanded = 1 << 1,

    /// <summary>Группа профиля: двуручная ближняя форма.</summary>
    TwoHanded = 1 << 2,

    /// <summary>Группа профиля: дальнобойная форма.</summary>
    Ranged = 1 << 3,

    /// <summary>Меч.</summary>
    Sword = 1 << 4,

    /// <summary>Лук или арбалет.</summary>
    BowOrCrossbow = 1 << 5,

    /// <summary>Клинковое оружие.</summary>
    Bladed = 1 << 6,

    /// <summary>Дробящее оружие.</summary>
    BluntOrCrushing = 1 << 7,

    /// <summary>У оружия есть рабочая режущая кромка.</summary>
    HasCuttingEdge = 1 << 8,

    /// <summary>Рабочая кромка деревянная.</summary>
    WoodenWorkingEdge = 1 << 9,
}
