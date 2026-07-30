namespace GenesysForge.Domain;

/// <summary>
/// Игровая категория товара в общей витрине. Это не тип хранения: услуга использует существующую
/// запись справочника, но покупается отдельной командой и не становится предметом.
/// </summary>
public enum ShopItemCategory
{
    WeaponLight,
    WeaponHeavy,
    WeaponRanged,
    MagicImplement,
    Armor,
    Transport,
    Gear,
    Consumable,
    Service,
}
