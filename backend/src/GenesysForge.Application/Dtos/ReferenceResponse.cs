namespace GenesysForge.Application.Dtos;

public record ReferenceResponse(
    List<ArchetypeDto> Archetypes,
    List<CareerDto> Careers,
    List<SkillDefDto> Skills,
    List<TalentDefDto> Talents,
    List<ItemDefDto> Items,
    List<HeroicAbilityDto> HeroicAbilities,
    List<QualityDto> Qualities,
    List<HeroicSecondaryEffectDto> HeroicSecondaryEffects);

public record HeroicSecondaryEffectDto(
    Guid Id, string Code, string Name, string NameRu, string Description, string SafeDescription,
    string Source, string DescriptionEn = "");

public record QualityDto(
    Guid Id, string Code, string NameEn, string NameRu, GenesysForge.Domain.Entities.QualityKind Kind,
    bool IsActive, bool HasRating, string ActivationCost, string Category,
    string Description, string SafeDescription, string Source, string DescriptionEn = "");
