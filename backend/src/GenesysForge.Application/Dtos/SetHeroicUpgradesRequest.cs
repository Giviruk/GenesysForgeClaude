namespace GenesysForge.Application.Dtos;

/// <summary>Полная целевая конфигурация улучшений героической способности.</summary>
public record SetHeroicUpgradesRequest(
    int PowerRank,
    int DurationRanks,
    int FrequencyRanks,
    bool Story,
    List<Guid> SecondaryEffectIds);
