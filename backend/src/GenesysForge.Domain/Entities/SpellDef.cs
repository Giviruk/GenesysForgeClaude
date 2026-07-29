namespace GenesysForge.Domain.Entities;

/// <summary>
/// Запись справочника магии Genesys: базовый эффект заклинания (направление) или
/// дополнительный эффект-модификатор. Полные тексты книг не хранятся — только
/// структура, числовые параметры и краткие парафраз-описания.
/// </summary>
public class SpellDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Магический навык: Arcana, Divine, Primal, Runes, Verse.</summary>
    public required string MagicSkill { get; set; }
    public SpellEntryKind Kind { get; set; }
    /// <summary>
    /// Для дополнительного эффекта (Kind=AdditionalEffect) — стабильный код (NameEn) базового
    /// эффекта, к которому он относится. У базовых эффектов пусто.
    /// </summary>
    public string ParentEffect { get; set; } = "";
    public required string NameRu { get; set; }
    public required string NameEn { get; set; }
    /// <summary>Отображаемая сложность: для эффектов — базовая, для модификаторов — «+N».</summary>
    public string Difficulty { get; set; } = "";

    /// <summary>
    /// Число из <see cref="Difficulty"/>: базовая сложность действия или надбавка дополнительного
    /// эффекта. Хранится отдельно, потому что считать по печатной строке — значит разбирать текст
    /// там, где нужно правило (ROT-MAG-01).
    /// </summary>
    public int DifficultyIncrease { get; set; }

    /// <summary>
    /// Магические направления, которым доступна запись, через запятую: у действия — его строка
    /// матрицы, у дополнительного эффекта — доступность родительского действия, суженная явным
    /// ограничением. Пусто быть не должно: недоступная всем запись в справочник не попадает.
    /// </summary>
    public string AllowedSkills { get; set; } = "";

    /// <summary>
    /// Коды эффектов, вместе с которыми этот выбрать нельзя, через запятую. Ограничение структурное,
    /// а не оговорка внутри описания.
    /// </summary>
    public string Exclusions { get; set; } = "";

    /// <summary>
    /// Как эффект применяется: сразу при успехе, пассивным свойством, свойством с активацией,
    /// тратой преимуществ после броска, очком сюжета или повествовательно.
    /// </summary>
    public SpellResolutionKind Resolution { get; set; }

    /// <summary>
    /// Запись из опциональной книги (Expanded Player's Guide). RoT-матрицу такие действия не
    /// меняют, и в интерфейсе они помечены.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Числа эффекта берутся из рангов Знания заклинателя (ROT-MAG-10): в RoT это
    /// <c>Knowledge (Lore)</c>, а с талантом «Тёмное прозрение» игрок может считать по
    /// <c>Knowledge (Forbidden)</c>.
    /// </summary>
    public bool UsesKnowledgeRating { get; set; }

    /// <summary>
    /// Коды свойств, которые получают этот рейтинг, через запятую. Пусто при
    /// <see cref="UsesKnowledgeRating"/> означает, что по Знанию считается само число эффекта, а не
    /// рейтинг свойства. Свойства без рейтинга (Повреждение, Нокдаун) сюда не попадают: «Повреждение N»
    /// не существует.
    /// </summary>
    public string RatedQualities { get; set; } = "";

    /// <summary>
    /// Навык, которому эффект доступен исключительно («Только Вера», «Только Магия»); пусто —
    /// доступен нескольким. Признак структурный, а не вычитанный из описания: по нему считается
    /// скидка священного символа (ROT-MAG-IMP-01), и разбирать текст правила для этого нельзя.
    /// </summary>
    public string RestrictedSkill { get; set; } = "";

    /// <summary>
    /// Эффект можно добавить к одному заклинанию несколько раз, и каждое добавление снова
    /// повышает сложность: Дистанция удлиняет дальность на категорию за раз, Размер — силуэт.
    /// Признак структурный: сборщик по нему разрешает счётчик вместо галочки.
    /// </summary>
    public bool Repeatable { get; set; }
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode=PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел (без копирования текста). Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    public int SortOrder { get; set; }
    public Guid? OwnerUserId { get; set; }
}
