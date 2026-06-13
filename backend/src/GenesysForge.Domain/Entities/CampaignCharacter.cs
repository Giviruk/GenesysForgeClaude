namespace GenesysForge.Domain.Entities;

/// <summary>Персонаж, участвующий в кампании (добавлен своим владельцем-игроком).</summary>
public class CampaignCharacter
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid CharacterId { get; set; }
    public Character? Character { get; set; }
    /// <summary>Владелец персонажа (игрок).</summary>
    public Guid PlayerUserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
