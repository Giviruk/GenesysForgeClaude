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

    /// <summary>
    /// Состояние повреждения экземпляра (GEN-EQP-DMG-01). Меняется отдельным действием — и когда
    /// в бою сработал Sunder, и когда предмет пострадал по сюжету. Legacy-строки — целые.
    /// </summary>
    public ItemDamageState DamageState { get; set; } = ItemDamageState.Undamaged;

    /// <summary>
    /// Материал магического инструмента (ROT-MAG-MAT-01). Выбирается один раз при изготовлении и
    /// дальше неизменен. У всего остального — дуб, то есть числа каталога без изменений.
    /// </summary>
    public ImplementMaterial ImplementMaterial { get; set; } = ImplementMaterial.Oak;

    /// <summary>
    /// Эффекты, выбранные ведущим у фолианта или палочки (ROT-MAG-IMP-01), кодами через запятую.
    /// Пусто — выбор ещё не сделан.
    /// </summary>
    public string ImplementChoices { get; set; } = "";

    /// <summary>
    /// Ведущий подтвердил выбор эффектов. Пока нет — обычные числа инструмента работают, а
    /// бесплатный эффект нет: витрина не может выдать полностью рабочий фолиант сама.
    /// </summary>
    public bool ImplementConfigured { get; set; }

    /// <summary>
    /// Малый полезный activation effect Lesser Rune, один раз сформулированный ведущим.
    /// У прочих предметов пусто.
    /// </summary>
    public string ShardActivationChoice { get; set; } = "";

    /// <summary>
    /// Код одного additional effect с печатной надбавкой +1, выбранного для Lesser Rune.
    /// </summary>
    public string ShardEffectChoice { get; set; } = "";

    /// <summary>Magic action, которому принадлежит выбранный additional effect Lesser Rune.</summary>
    public string ShardEffectAction { get; set; } = "";

    /// <summary>
    /// Lesser Rune настроена. После этого обычная команда конфигурации не меняет выбор повторно.
    /// </summary>
    public bool ShardConfigured { get; set; }
}
