using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record SpellDto(Guid Id, string MagicSkill, SpellEntryKind Kind, string NameRu, string NameEn,
    string Difficulty, string Description, bool IsCustom);
