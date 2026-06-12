using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record ItemDefDto(Guid Id, string Name, ItemKind Kind, int Encumbrance, int SoakBonus, int MeleeDefense,
    int RangedDefense, int EncumbranceThresholdBonus, string Description, int Price, int Rarity, bool IsCustom);
