namespace GenesysForge.Domain.Entities;

/// <summary>Стандартный вторичный эффект для улучшения героической способности RoT.</summary>
public class HeroicSecondaryEffectDef : IContentDef
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public required string Name { get; set; }
    public string NameRu { get; set; } = "";
    public string Description { get; set; } = "";
    public string SafeDescription { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public string Source { get; set; } = "";
}
