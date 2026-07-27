namespace GenesysForge.Domain.Entities;

public class CharacterItem
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid ItemDefId { get; set; }
    public ItemDef? ItemDef { get; set; }
    public int Quantity { get; set; } = 1;
    public ItemState State { get; set; } = ItemState.Carried;
    /// <summary>Откуда позиция появилась: покупка, карьерный комплект, стартовый бюджет или импорт.</summary>
    public ItemProvenance Provenance { get; set; } = ItemProvenance.Purchased;

    /// <summary>
    /// Оружие метнули и ещё не подобрали (ROT-WPN-01, Limited Ammo метательного профиля). Экземпляр
    /// недоступен для атак и не даёт своих качеств, но не исчезает: он лежит там, куда улетел.
    /// </summary>
    public bool IsThrown { get; set; }

    /// <summary>
    /// Качество изготовления экземпляра (ROT-WPN-02). Выбирается один раз при добавлении и дальше
    /// не меняется: типы не складываются, а перековать вещь правила не дают. Legacy-строки и
    /// снаряжение — <see cref="WeaponCraftsmanship.Steel"/>, то есть числа таблицы без изменений.
    /// </summary>
    public WeaponCraftsmanship Craftsmanship { get; set; } = WeaponCraftsmanship.Steel;
}
