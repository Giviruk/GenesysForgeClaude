namespace GenesysForge.Domain;

/// <summary>Боевая/социальная роль NPC. Помогает генерировать черновики и фильтровать библиотеку.</summary>
public enum NpcRole
{
    /// <summary>Сильный ближник с высоким Brawn и Soak.</summary>
    Brute = 0,
    /// <summary>Быстрый мобильный противник.</summary>
    Skirmisher = 1,
    /// <summary>Дальний атакующий.</summary>
    Archer = 2,
    /// <summary>Маг или мистик.</summary>
    Caster = 3,
    /// <summary>Командир, усиливающий других.</summary>
    Leader = 4,
    /// <summary>Переговорщик, интриган, политик.</summary>
    Social = 5,
    /// <summary>Лекарь, баффер, помощник.</summary>
    Support = 6,
    /// <summary>Зверь или чудовище.</summary>
    Monster = 7,
    /// <summary>Произвольная роль.</summary>
    Custom = 8,
}
