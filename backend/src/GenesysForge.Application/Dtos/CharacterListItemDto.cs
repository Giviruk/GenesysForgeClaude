using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CharacterListItemDto(
    Guid Id,
    string Name,
    GameSystem System,
    string Archetype,
    string Career,
    bool IsCreationPhase,
    DateTime CreatedAt,
    int AvailableXp,
    int WoundsCurrent,
    int WoundThreshold,
    int StrainCurrent,
    int StrainThreshold);
