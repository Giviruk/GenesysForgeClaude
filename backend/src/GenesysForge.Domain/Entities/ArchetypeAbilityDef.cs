namespace GenesysForge.Domain.Entities;

/// <summary>
/// Видовая способность архетипа как данные (а не только текст в SafeDescription). Отображается при
/// выборе вида. Исполнение эффектов — задача U-18; здесь <see cref="AutomationKind"/> — только тег.
/// </summary>
public class ArchetypeAbilityDef
{
    public Guid Id { get; set; }
    public Guid ArchetypeId { get; set; }
    /// <summary>Стабильный код способности (для будущего движка эффектов).</summary>
    public string Code { get; set; } = "";
    /// <summary>Русское название способности.</summary>
    public string NameRu { get; set; } = "";
    /// <summary>Оригинальное/английское название (может быть пустым, если в источнике нет).</summary>
    public string NameEn { get; set; } = "";
    /// <summary>Copyright-safe краткое описание-парафраз.</summary>
    public string SafeDescription { get; set; } = "";
    public ArchetypeAbilityAutomationKind AutomationKind { get; set; } = ArchetypeAbilityAutomationKind.Manual;
}
