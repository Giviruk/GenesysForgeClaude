namespace GenesysForge.Domain.Entities;

public class HomebrewPackCampaign
{
    public Guid Id { get; set; }
    public Guid HomebrewPackId { get; set; }
    public Guid CampaignId { get; set; }
    public bool IsEnabled { get; set; }
    public ContentUpdatePolicy UpdatePolicy { get; set; } = ContentUpdatePolicy.Auto;
    public ContentConnectionStatus Status { get; set; } = ContentConnectionStatus.Active;
    public Guid? ProposedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
