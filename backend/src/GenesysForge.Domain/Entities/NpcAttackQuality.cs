namespace GenesysForge.Domain.Entities;

/// <summary>
/// Качество атаки NPC. Ссылается на справочник <see cref="QualityDef"/> (U-10) по необязательному FK;
/// код и русское имя денормализуются для отображения и поддержки кастомных (не из каталога) качеств.
/// </summary>
public class NpcAttackQuality
{
    public Guid Id { get; set; }
    public Guid NpcAttackId { get; set; }

    /// <summary>Справочное качество, если выбрано из каталога; null для кастомного.</summary>
    public Guid? QualityDefId { get; set; }
    public QualityDef? QualityDef { get; set; }

    /// <summary>Код качества (slug). Для каталожного = <see cref="QualityDef.Code"/>, для кастомного — свободный.</summary>
    public string QualityCode { get; set; } = "";
    /// <summary>Русское имя качества для отображения (денормализовано).</summary>
    public string NameRu { get; set; } = "";
    /// <summary>Рейтинг качества (если у качества есть рейтинг), иначе null.</summary>
    public int? Rating { get; set; }
}
