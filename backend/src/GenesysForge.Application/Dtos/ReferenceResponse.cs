namespace GenesysForge.Application.Dtos;

public record ReferenceResponse(
    List<ArchetypeDto> Archetypes,
    List<CareerDto> Careers,
    List<SkillDefDto> Skills,
    List<TalentDefDto> Talents,
    List<ItemDefDto> Items,
    List<HeroicAbilityDto> HeroicAbilities);
