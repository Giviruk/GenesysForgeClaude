namespace GenesysForge.Domain.Entities;

public class ItemDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Стабильный код встроенного контента. У кастома пусто.</summary>
    public string Code { get; set; } = "";
    /// <summary>Оригинальное/английское название.</summary>
    public required string Name { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";
    public ItemKind Kind { get; set; }
    public int Encumbrance { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }
    public int EncumbranceThresholdBonus { get; set; }
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме ContentMode.PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode.PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел/страница. Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    /// <summary>
    /// Обычная каталожная цена. <c>null</c> означает, что книга не задаёт цену; это не ноль монет.
    /// </summary>
    public int? Price { get; set; }

    /// <summary>
    /// Обычная редкость. <c>null</c> означает, что книга не задаёт значение; это не редкость 0.
    /// </summary>
    public int? Rarity { get; set; }

    /// <summary>Запись можно купить через обычную витрину за каталожную цену.</summary>
    public bool Purchasable { get; set; } = true;

    /// <summary>Экземпляр можно продать через обычную экономику персонажа.</summary>
    public bool Sellable { get; set; } = true;

    /// <summary>
    /// Слоты улучшений (hard points) по таблице книги (ROT-WPN-01/ROT-ARM-01). Берётся из таблицы,
    /// а не вычисляется из веса. <c>null</c> — у записи нет книжного значения (ещё не перенесено),
    /// и это не то же самое, что «ноль слотов».
    /// </summary>
    public int? HardPoints { get; set; }

    /// <summary>
    /// Признаки формы предмета (ROT-EQP-ATT-01): меч, клинковое, дробящее, латное, металлическое.
    /// Совместимость улучшений считается по ним, а не разбором названия — «Клинок бури» может
    /// оказаться дубиной.
    /// </summary>
    public WeaponFormTraits FormTraits { get; set; }

    /// <summary>
    /// Влияние предмета на проверки навыков: штраф Скрытности у тяжёлой брони и подобное
    /// (ROT-ARM-01). Пусто — предмет проверки не меняет.
    /// </summary>
    public List<ItemCheckModifier> CheckModifiers { get; set; } = [];

    /// <summary>
    /// Типизированные профили атаки (ROT-WPN-01): основной и альтернативные (метание, в руке).
    /// Пусто — у записи структурного профиля нет, числа читаются из строковых полей выше.
    /// </summary>
    public List<WeaponAttackProfile> AttackProfiles { get; set; } = [];

    // ── Боевые характеристики (заполнены только у оружия) ──
    /// <summary>Английское имя боевого навыка для броска (например, «Melee (Light)», «Ranged»). У не-оружия пусто.</summary>
    public string SkillName { get; set; } = "";
    /// <summary>Урон: «+3» (прибавка к Мощи для ближнего боя) или абсолютное число.</summary>
    public string Damage { get; set; } = "";
    /// <summary>Критическое значение.</summary>
    public string Crit { get; set; } = "";
    /// <summary>Дистанция (русская подпись): «Вплотную», «Средняя» и т. п.</summary>
    public string RangeBand { get; set; } = "";
    /// <summary>Свойства/эффекты оружия (русские). Сохраняется как исходный fallback к структурным <see cref="Qualities"/>.</summary>
    public string Properties { get; set; } = "";

    /// <summary>Структурные качества (свойство+рейтинг). Бэкфилятся из <see cref="Properties"/> у встроенных предметов.</summary>
    public List<ItemQualityValue> Qualities { get; set; } = [];

    public Guid? OwnerUserId { get; set; }
    public Guid? HomebrewPackId { get; set; }
    /// <summary>
    /// Запись исключена из новых выборов, но сохранена ради существующих ссылок
    /// (см. <see cref="IContentDef.Retired"/>).
    /// </summary>
    public bool Retired { get; set; }
}
