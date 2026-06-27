namespace GenesysForge.Domain;

/// <summary>
/// Шаблон типа существа для генератора черновиков NPC (U-16). Влияет только на генерацию
/// (теги, тематические способности, природные атаки, terror/иммунитеты), не хранится в модели Npc —
/// тип выражается тегами и способностями результата.
/// </summary>
public enum CreatureTemplate
{
    /// <summary>Обычный гуманоид — без шаблона.</summary>
    None = 0,
    /// <summary>Нежить: иммунитеты, Ужас.</summary>
    Undead = 1,
    /// <summary>Зверь: природное оружие.</summary>
    Beast = 2,
    /// <summary>Дракон: крупный, Ужас, дыхание.</summary>
    Dragon = 3,
    /// <summary>Демон: Ужас, магическое сопротивление.</summary>
    Demon = 4,
    /// <summary>Конструкт: иммунитеты к яду/усталости/страху.</summary>
    Construct = 5,
}
