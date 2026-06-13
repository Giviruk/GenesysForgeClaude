namespace GenesysForge.Domain.Entities;

/// <summary>Игровая кампания, которую ведёт мастер (GM).</summary>
public class Campaign
{
    public Guid Id { get; set; }
    public Guid GmUserId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    /// <summary>Код присоединения: игрок добавляет своего персонажа по этому коду.</summary>
    public required string JoinCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CampaignCharacter> Characters { get; set; } = [];
    public List<CampaignNote> Notes { get; set; } = [];
}
