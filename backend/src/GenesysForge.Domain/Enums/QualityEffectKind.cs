namespace GenesysForge.Domain;

/// <summary>
/// Что качество делает механически (GEN-EQP-QUAL-01). До этого механика выводилась из названия
/// или из текста описания, поэтому Точное и Громоздкое были просто надписями на карточке.
/// </summary>
public enum QualityEffectKind
{
    /// <summary>
    /// Эффект приложением не исполняется: ему нужен рантайм столкновения (раунды, статусы,
    /// повторные срабатывания). Текст и метаданные при этом верные.
    /// </summary>
    Descriptive = 0,

    /// <summary>Прибавляет к атаке этим оружием бонусные кости по рейтингу (Accurate).</summary>
    AttackBoost = 1,

    /// <summary>Прибавляет к атаке этим оружием кости помех по рейтингу (Inaccurate).</summary>
    AttackSetback = 2,

    /// <summary>Мощь меньше рейтинга — сложность растёт на разницу (Cumbersome).</summary>
    DifficultyPerMissingBrawn = 3,

    /// <summary>Ловкость меньше рейтинга — сложность растёт на разницу (Unwieldy).</summary>
    DifficultyPerMissingAgility = 4,

    /// <summary>Автоматическое преимущество к каждой проверке с предметом (Superior).</summary>
    AutomaticAdvantage = 5,

    /// <summary>Автоматическая угроза к каждой проверке с предметом (Inferior).</summary>
    AutomaticThreat = 6,

    /// <summary>Надбавка к ближней защите, пока предмет в руках (Defensive).</summary>
    DefenseMelee = 7,

    /// <summary>Надбавка к дальней защите, пока предмет в руках (Deflection).</summary>
    DefenseRanged = 8,

    /// <summary>Игнорирует поглощение цели по рейтингу (Pierce).</summary>
    IgnoreSoak = 9,

    /// <summary>Игнорирует десятикратный рейтинг поглощения цели (Breach).</summary>
    IgnoreSoakTenfold = 10,

    /// <summary>Броня не поддаётся Pierce/Breach, а предмет — Sunder (Reinforced).</summary>
    ImmuneToPierceAndSunder = 11,

    /// <summary>Прибавляет 10 × рейтинг к броску критического ранения (Vicious).</summary>
    CriticalBonusTenfold = 12,
}

/// <summary>Сколько раз качество можно применить в одной атаке (GEN-EQP-QUAL-01).</summary>
public enum QualityRepeatability
{
    /// <summary>Один раз за атаку.</summary>
    Once = 0,

    /// <summary>По разу на каждое дополнительное попадание (Auto-fire, Linked).</summary>
    PerAdditionalHit = 1,

    /// <summary>По разу на каждую поражённую цель (Concussive, Burn).</summary>
    PerHitTarget = 2,
}
