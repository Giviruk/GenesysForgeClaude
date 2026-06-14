namespace GenesysForge.Application.Dtos;

public record ArchetypeDto(Guid Id, string Name, string NameRu, int Brawn, int Agility, int Intellect, int Cunning,
    int Willpower, int Presence, int WoundBase, int StrainBase, int StartingXp,
    string Description, string SafeDescription, string Source);
