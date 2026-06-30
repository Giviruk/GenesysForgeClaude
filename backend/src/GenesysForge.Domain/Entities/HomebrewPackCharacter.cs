namespace GenesysForge.Domain.Entities;

public class HomebrewPackCharacter
{
    public Guid Id { get; set; }
    public Guid HomebrewPackId { get; set; }
    public Guid CharacterId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
