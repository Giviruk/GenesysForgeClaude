namespace GenesysForge.Domain.Entities;

public class ArchetypeDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public int Brawn { get; set; }
    public int Agility { get; set; }
    public int Intellect { get; set; }
    public int Cunning { get; set; }
    public int Willpower { get; set; }
    public int Presence { get; set; }
    public int WoundBase { get; set; }
    public int StrainBase { get; set; }
    public int StartingXp { get; set; }
    public string Description { get; set; } = "";
}
