namespace GenesysForge.Domain.Entities;

/// <summary>Вид справочной таблицы правил (для группировки и фильтра в UI).</summary>
public enum RuleTableKind
{
    /// <summary>Ладдер сложностей (Simple…Formidable, число кубов сложности).</summary>
    Difficulty,
    /// <summary>Траты Advantage/Threat/Triumph/Despair по ситуациям (бой/социалка/прочее).</summary>
    SymbolSpend,
    /// <summary>Диапазоны дистанций (range bands).</summary>
    RangeBand,
    /// <summary>Таблица критических ранений (d100).</summary>
    CriticalInjury,
    /// <summary>Свойства оружия и предметов, применимые к оружию.</summary>
    WeaponProperty,
    /// <summary>Боевые действия и манёвры персонажа.</summary>
    CombatActionManeuver,
    /// <summary>Магические действия и манёвры заклинателя.</summary>
    MagicActionManeuver,
}
