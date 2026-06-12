using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CreateCustomTalentRequest(GameSystem System, string Name, int Tier, bool IsRanked, string Activation,
    string Description, int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus);
