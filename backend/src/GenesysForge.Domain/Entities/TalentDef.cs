namespace GenesysForge.Domain.Entities;

public class TalentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public int Tier { get; set; }
    public bool IsRanked { get; set; }
    public string Description { get; set; } = "";
    public string Activation { get; set; } = "Пассивный";
    // Пассивные бонусы, применяемые автоматически за каждый ранг.
    public int WoundBonus { get; set; }
    public int StrainBonus { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefenseBonus { get; set; }
    public int RangedDefenseBonus { get; set; }
    public Guid? OwnerUserId { get; set; }
}
