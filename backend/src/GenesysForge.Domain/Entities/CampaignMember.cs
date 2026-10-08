namespace GenesysForge.Domain.Entities;

/// <summary>Аккаунт игрока в кампании. Мастер определяется Campaign.GmUserId.</summary>
public class CampaignMember
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
