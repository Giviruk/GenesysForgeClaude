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

    /// <summary>Полное (private) описание-парафраз. Очищается в PublicSafe.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание.</summary>
    public string SafeDescription { get; set; } = "";
    public string Source { get; set; } = "";
}
