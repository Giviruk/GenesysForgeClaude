namespace GenesysForge.Domain.Entities;

/// <summary>Особая способность NPC: название и описание.</summary>
public class NpcAbility
{
    public Guid Id { get; set; }
    public Guid NpcId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
}
