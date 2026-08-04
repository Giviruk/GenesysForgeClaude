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
    /// Позиция лежит на транспорте, а не при персонаже (ROT-TRANSPORT-01). Такая позиция не входит
    /// ни в переносимый вес владельца, ни в его надетое снаряжение — поэтому попона, надетая на
    /// скакуна, не даёт защиту всаднику. <c>null</c> — обычная позиция инвентаря.
    /// </summary>
    public Guid? CarriedByMountId { get; set; }
    public CharacterMount? CarriedByMount { get; set; }

    /// <summary>
    /// Снаряжение установлено на транспорт, а не сложено в него грузом: попона и седельные сумки.
    /// Установленное не занимает вместимость, а меняет её и защиту самого транспорта.
    /// Осмысленно только вместе с <see cref="CarriedByMountId"/>.
    /// </summary>
    public bool IsInstalledOnMount { get; set; }

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

    /// <summary>
    /// Экземпляр изготовлен персонажем (ROT-CRAFT-01). Хранит проект, из которого он вышел, —
    /// по нему видно бросок, траты и стоимость. <c>null</c> — предмет куплен или выдан.
    /// </summary>
    public Guid? CraftingProjectId { get; set; }

    /// <summary>Поправка веса от трат символов при изготовлении.</summary>
    public int CraftedEncumbrance { get; set; }

    /// <summary>Поправка слотов улучшений от трат символов при изготовлении.</summary>
    public int CraftedHardPoints { get; set; }

    /// <summary>
    /// Качества, добавленные изготовлением, кодами через запятую с рейтингом
    /// (<c>superior,inaccurate:1</c>). Тот же способ хранения, что у выборов инструмента.
    /// </summary>
    public string CraftedQualities { get; set; } = "";

    /// <summary>Хрупкая работа: при повреждении экземпляр теряет на одну ступень больше.</summary>
    public bool CraftedFragile { get; set; }

    /// <summary>
    /// Описание изготовления: все выбранные траты символов словами. Показывается на карточке
    /// предмета, потому что половина трат — правила, которые приложение не исполняет.
    /// </summary>
    public string CraftNote { get; set; } = "";
}
