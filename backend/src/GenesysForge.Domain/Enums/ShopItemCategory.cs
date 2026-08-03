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

    /// <summary>
    /// Каталожная реликвия (ROT-MITEM-01). Стоит отдельно от оружия и снаряжения: её не покупают,
    /// а выдаёт ведущий, и искать её в витрине среди обычных мечей бессмысленно.
    /// </summary>
    MagicItem,
    Armor,
    Transport,
    Gear,
    Consumable,
    Service,
}
