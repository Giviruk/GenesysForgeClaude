namespace GenesysForge.Application.Dtos;

public record TalentDefDto(Guid Id, string Name, int Tier, bool IsRanked, string Activation, string Description,
    int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus, bool IsCustom);
