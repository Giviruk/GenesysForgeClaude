using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record TalentDefDto(Guid Id, string Name, string NameRu, int Tier, bool IsRanked, TalentCategory Category, GenesysSetting Setting,
    string Activation, string Description, string SafeDescription, string Source,
    int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus, bool IsCustom,
    bool GrantsCharacteristic, string DescriptionEn = "");
