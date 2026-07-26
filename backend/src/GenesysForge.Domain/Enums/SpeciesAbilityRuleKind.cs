namespace GenesysForge.Domain;

/// <summary>
/// Исполняемый тип видовой способности (ROT-SPECIES-01). Эффект определяется этим полем и
/// структурными параметрами; распознавать способность по имени или тексту описания запрещено.
/// </summary>
public enum SpeciesAbilityRuleKind
{
    /// <summary>Правило без автоматизации — только текст и подсказка.</summary>
    Manual = 0,

    /// <summary>Human «Ready for Adventure»: раз за сессию переносит Story Point из пула GM в пул игроков.</summary>
    MoveStoryPointToPlayers = 1,

    /// <summary>Nimble: устанавливает базовые melee и ranged Defense в 1 (provider, а не +1).</summary>
    SetBaseDefense = 2,

    /// <summary>Dark Vision: убирает до N Setback с причиной <c>darkness</c> из собственной проверки.</summary>
    RemoveSetbackBySource = 3,

    /// <summary>Tough as Nails: раз за сессию за 1 Story Point считает бросок Critical Injury равным 01.</summary>
    ForceCriticalInjuryRoll = 4,

    /// <summary>Stubborn: добавляет Setback к социальной проверке, нацеленной на владельца.</summary>
    AddSetbackWhenTargeted = 5,

    /// <summary>Battle Rage: явный pre-roll выбор — Setback к проверке ради +N урона одному попаданию.</summary>
    OptionalSetbackForDamage = 6,

    /// <summary>Hot Tempered: пока strain строго выше половины ST — Setback к социальным и +N урона в ближнем бою.</summary>
    StrainThresholdRage = 7,

    /// <summary>Tenacious: после первого попадания по цели даёт Boost против этой же цели до конца encounter.</summary>
    BoostAgainstMarkedTarget = 8,

    /// <summary>Claws: виртуальная natural attack, а не покупаемый предмет.</summary>
    NaturalWeapon = 9,

    /// <summary>Fleet of Paw: отменяет strain за второй manoeuvre, если он является перемещением.</summary>
    FreeSecondMoveManeuver = 10,

    /// <summary>Small: silhouette 0.</summary>
    SetSilhouette = 11,

    /// <summary>Militia Training: Boost к combat check, если silhouette цели строго больше своей.</summary>
    BoostAgainstLargerSilhouette = 12,

    /// <summary>Tricksy: раз за encounter за 1 Story Point выдаёт предмет в пределах Enc/Rarity.</summary>
    ConjureMinorItem = 13,

    /// <summary>Half-Catfolk: обязательный неизменяемый выбор одной из перечисленных способностей.</summary>
    ChooseOneAbility = 14,

    /// <summary>Выдача карьерного навыка и стартовых рангов — уже описана стартовыми навыками вида.</summary>
    SkillGrantOnly = 15,
}

/// <summary>
/// Область, в которой сбрасывается счётчик использований способности или таланта (ROT-TAL-05).
/// Области различаются: once/session не сбрасывается новым encounter, once/encounter — только
/// началом encounter, once/round — новым раундом.
/// </summary>
public enum AbilityUseScope
{
    /// <summary>Счётчик не ведётся — эффект пассивный или без лимита.</summary>
    None = 0,
    /// <summary>Сбрасывается в конце encounter.</summary>
    Encounter = 1,
    /// <summary>Сбрасывается в конце игровой сессии.</summary>
    Session = 2,
    /// <summary>Сбрасывается с началом нового раунда.</summary>
    Round = 3,
    /// <summary>Сбрасывается с началом нового хода владельца.</summary>
    Turn = 4,
}
