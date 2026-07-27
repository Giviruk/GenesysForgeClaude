namespace GenesysForge.Domain.Entities;

/// <summary>
/// Справочное качество (свойство предмета/оружия/брони). Системо-независимо — одно определение
/// на качество, на которое ссылаются предметы обеих систем через <see cref="ItemQualityValue"/>.
/// </summary>
public class QualityDef : IContentDef
{
    public const int MaxActivationCostLength = 400;

    public Guid Id { get; set; }
    /// <summary>Стабильный код (slug английского имени).</summary>
    public string Code { get; set; } = "";
    /// <summary>Английское название (как в Core Rulebook).</summary>
    public required string NameEn { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";

    public QualityKind Kind { get; set; } = QualityKind.ItemQuality;
    /// <summary>Активное (требует траты при срабатывании) или пассивное.</summary>
    public bool IsActive { get; set; }
    /// <summary>Есть ли у качества числовой рейтинг.</summary>
    public bool HasRating { get; set; }
    /// <summary>Базовая трата активации (например «2 преимущества»).</summary>
    public string ActivationCost { get; set; } = "";
    /// <summary>Категория (оружие/броня/...).</summary>
    public string Category { get; set; } = "";

    // ── Структурные метаданные (GEN-EQP-QUAL-01) ──
    // Раньше всё это жило только в тексте активации, поэтому проверить или посчитать было нечего.

    /// <summary>Что качество делает механически. <c>Descriptive</c> — исполнения пока нет.</summary>
    public QualityEffectKind EffectKind { get; set; } = QualityEffectKind.Descriptive;

    /// <summary>
    /// Сколько преимуществ стоит активация. У пассивного качества ноль. По умолчанию активное
    /// стоит два — исключения (Sunder 1, Guided 3) заданы явно.
    /// </summary>
    public int AdvantageCost { get; set; }

    /// <summary>Активация требует попадания. По умолчанию у активных качеств — да.</summary>
    public bool RequiresHit { get; set; }

    /// <summary>Правило прямо разрешает активацию при промахе (Blast за 3 преимущества).</summary>
    public bool CanActivateOnMiss { get; set; }

    /// <summary>Триумф может оплатить активацию вместо преимуществ.</summary>
    public bool TriumphMayPay { get; set; }

    /// <summary>Сколько раз качество применимо в одной атаке.</summary>
    public QualityRepeatability Repeatability { get; set; } = QualityRepeatability.Once;

    /// <summary>Полное (private) описание-парафраз. Очищается в PublicSafe.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание.</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    public string Source { get; set; } = "";
    /// <summary>
    /// Запись исключена из новых выборов, но сохранена ради существующих ссылок
    /// (см. <see cref="IContentDef.Retired"/>).
    /// </summary>
    public bool Retired { get; set; }
}
