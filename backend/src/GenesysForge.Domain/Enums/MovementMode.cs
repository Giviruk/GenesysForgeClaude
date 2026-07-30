namespace GenesysForge.Domain;

/// <summary>
/// Режим движения транспорта (ROT-TRANSPORT-01). Числовой скорости книга этим профилям не даёт,
/// поэтому карточка показывает именно режим, а не выдуманную величину.
/// </summary>
public enum MovementMode
{
    /// <summary>По земле своим ходом.</summary>
    Ground = 0,

    /// <summary>По воздуху: непроходимая по земле местность не тратит движение.</summary>
    Flight = 1,

    /// <summary>На колёсах: своего хода нет, нужна тяга.</summary>
    Wheeled = 2,
}
