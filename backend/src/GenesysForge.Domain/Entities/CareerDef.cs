namespace GenesysForge.Domain.Entities;

public class CareerDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public List<string> CareerSkillNames { get; set; } = [];
}
