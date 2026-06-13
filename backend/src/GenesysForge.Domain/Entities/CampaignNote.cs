namespace GenesysForge.Domain.Entities;

/// <summary>Заметка кампании, которую ведёт GM. Приватные заметки игрокам не видны.</summary>
public class CampaignNote
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = "";
    /// <summary>true — видна только GM; false — общая, видна участникам кампании.</summary>
    public bool IsPrivate { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
