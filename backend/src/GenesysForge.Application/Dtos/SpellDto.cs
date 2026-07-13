using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record SpellDto(Guid Id, string MagicSkill, SpellEntryKind Kind, string ParentEffect,
    string NameRu, string NameEn, string Difficulty, string Description, string SafeDescription,
    string Source, bool IsCustom, string DescriptionEn = "");
