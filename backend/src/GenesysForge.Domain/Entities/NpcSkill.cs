namespace GenesysForge.Domain.Entities;

/// <summary>Навык NPC: имя в свободной форме и количество рангов (0..5).</summary>
public class NpcSkill
{
    public Guid Id { get; set; }
    public Guid NpcId { get; set; }
    public required string Name { get; set; }
    public int Ranks { get; set; }
}
