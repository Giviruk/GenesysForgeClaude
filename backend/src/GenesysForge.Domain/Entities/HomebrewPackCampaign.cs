namespace GenesysForge.Domain.Entities;

public class HomebrewPackCampaign
{
    public Guid Id { get; set; }
    public Guid HomebrewPackId { get; set; }
    public Guid CampaignId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
