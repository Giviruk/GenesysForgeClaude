namespace GenesysForge.Domain.Entities;

/// <summary>Набор разрешённого/запрещённого контента кампании (Campaign Handbook).</summary>
public class ContentPack
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    /// <summary>Кампания, к которой привязан pack.</summary>
    public Guid CampaignId { get; set; }

    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public GameSystem System { get; set; }

    /// <summary>Опубликован ли handbook игрокам кампании (read-only).</summary>
    public bool IsPublicToCampaign { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ContentPackEntry> Entries { get; set; } = [];
}
