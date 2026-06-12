namespace GenesysForge.Domain;

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
