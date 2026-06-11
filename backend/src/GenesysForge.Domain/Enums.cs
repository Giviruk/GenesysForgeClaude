namespace GenesysForge.Domain;

/// <summary>Поддерживаемые НРИ-системы.</summary>
public enum GameSystem
{
    GenesysCore = 0,
    RealmsOfTerrinoth = 1,
}

/// <summary>Шесть характеристик Genesys.</summary>
public enum CharacteristicType
{
    Brawn = 0,
    Agility = 1,
    Intellect = 2,
    Cunning = 3,
    Willpower = 4,
    Presence = 5,
}

/// <summary>Состояние предмета в инвентаре.</summary>
public enum ItemState
{
    /// <summary>Используется (надето/в руках) — бонусы активны, броня даёт encumbrance −3.</summary>
    Equipped = 0,
    /// <summary>Не используется, но при себе — вес учитывается, бонусы не действуют.</summary>
    Carried = 1,
    /// <summary>В рюкзаке — вес учитывается, бонусы не действуют.</summary>
    Backpack = 2,
}

public enum ItemKind
{
    Weapon = 0,
    Armor = 1,
    Gear = 2,
}

public enum SkillKind
{
    General = 0,
    Combat = 1,
    Social = 2,
    Knowledge = 3,
    Magic = 4,
}
