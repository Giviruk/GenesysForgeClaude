namespace GenesysForge.Application.Dtos;

public record HeroicAbilityDto(Guid Id, string Name, string NameRu, string Description, string SafeDescription,
    string Source, bool IsCustom);
