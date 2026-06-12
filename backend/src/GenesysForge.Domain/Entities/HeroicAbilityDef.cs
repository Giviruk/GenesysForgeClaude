namespace GenesysForge.Domain.Entities;

public class HeroicAbilityDef
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public Guid? OwnerUserId { get; set; }
}
