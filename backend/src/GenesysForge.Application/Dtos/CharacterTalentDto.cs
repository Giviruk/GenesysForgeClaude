using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CharacterTalentDto(Guid TalentDefId, string Name, string NameRu, int Tier, bool IsRanked, int Ranks,
    string Activation, string Description,
    int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus,
    bool GrantsCharacteristic, IReadOnlyList<CharacteristicType> GrantedCharacteristics, string DescriptionEn = "");
