using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record ItemDefDto(Guid Id, string Name, string NameRu, ItemKind Kind, int Encumbrance, int SoakBonus,
    int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus,
    string Description, string SafeDescription, string Source, int Price, int Rarity,
    string SkillName, string Damage, string Crit, string RangeBand, string Properties, bool IsCustom);
