namespace GenesysForge.Domain.Entities;

public class CharacterTalent
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid TalentDefId { get; set; }
    public TalentDef? TalentDef { get; set; }
    public int Ranks { get; set; } = 1;
}
