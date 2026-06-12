namespace GenesysForge.Domain.Entities;

public class CharacterSkill
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid SkillDefId { get; set; }
    public SkillDef? SkillDef { get; set; }
    public int Ranks { get; set; }
    public bool IsCareer { get; set; }
}
