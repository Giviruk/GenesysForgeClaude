namespace GenesysForge.Domain.Entities;

/// <summary>
/// Запись Content Pack: ссылка на справочный контент или домашнее правило/заметка.
/// Для официального контента хранится только safe summary + источник (см. §10), без текста книг.
/// </summary>
public class ContentPackEntry
{
    public Guid Id { get; set; }
    public Guid ContentPackId { get; set; }

    public ContentEntryType ContentType { get; set; }
    /// <summary>Ссылка на существующую справочную сущность, если запись опирается на неё.</summary>
    public Guid? ContentId { get; set; }

    public required string Title { get; set; }
    public AllowedState AllowedState { get; set; } = AllowedState.Allowed;
    /// <summary>Категория для записей типа HouseRule (иначе None).</summary>
    public HouseRuleCategory Category { get; set; } = HouseRuleCategory.None;

    public string SafeSummary { get; set; } = "";
    public string Source { get; set; } = "";
    public string PageRef { get; set; } = "";
    /// <summary>Приватные заметки мастера (игрокам не видны).</summary>
    public string GmNotes { get; set; } = "";
    /// <summary>Заметки для игроков.</summary>
    public string PlayerNotes { get; set; } = "";

    public List<string> Tags { get; set; } = [];
    public int SortOrder { get; set; }
}
