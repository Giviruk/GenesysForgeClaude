namespace GenesysForge.Application.Dtos;

public record DerivedDto(int WoundThreshold, int StrainThreshold, int Soak, int MeleeDefense, int RangedDefense,
    int EncumbranceThreshold, int EncumbranceLoad, bool Encumbered,
    /// <summary>Как сложилась ближняя защита (ROT-CMB-03).</summary>
    DefenseBreakdownDto? MeleeDefenseBreakdown = null,
    /// <summary>Как сложилась дальняя защита.</summary>
    DefenseBreakdownDto? RangedDefenseBreakdown = null);

/// <summary>Один источник защиты для объяснения итога.</summary>
public record DefenseSourceDto(string SourceType, string SourceName, int Value);

/// <summary>
/// Разбор канала защиты: победивший провайдер, проигнорированные провайдеры (они не складываются),
/// надбавки и сырое значение до предела.
/// </summary>
public record DefenseBreakdownDto(
    int Raw,
    int Effective,
    bool Capped,
    DefenseSourceDto? Provider,
    List<DefenseSourceDto> IgnoredProviders,
    List<DefenseSourceDto> Increases);
