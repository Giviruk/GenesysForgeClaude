namespace GenesysForge.Domain.Entities;

/// <summary>Совместно редактируемая Markdown-глава хроники кампании.</summary>
public class CampaignChronicleChapter
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public required string Title { get; set; }
    public string Content { get; set; } = "";
    public int SortOrder { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public Guid CreatedByUserId { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CampaignChronicleRevision> Revisions { get; set; } = [];
}

/// <summary>Неизменяемый снимок главы для просмотра и безопасного восстановления.</summary>
public class CampaignChronicleRevision
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public int Version { get; set; }
    public required string Title { get; set; }
    public string Content { get; set; } = "";
    public Guid EditedByUserId { get; set; }
    public DateTime EditedAt { get; set; } = DateTime.UtcNow;
}
