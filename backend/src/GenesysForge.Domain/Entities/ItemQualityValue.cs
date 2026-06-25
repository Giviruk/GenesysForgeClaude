namespace GenesysForge.Domain.Entities;

/// <summary>Структурная привязка качества к предмету с необязательным рейтингом.</summary>
public class ItemQualityValue
{
    public Guid Id { get; set; }
    public Guid ItemDefId { get; set; }
    public Guid QualityDefId { get; set; }
    public QualityDef? QualityDef { get; set; }
    /// <summary>Рейтинг качества (если у качества есть рейтинг), иначе null.</summary>
    public int? Rating { get; set; }
}
