using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain;

/// <param name="MeleeDefenseBreakdown">
/// Как сложилась ближняя защита (ROT-CMB-03): победивший провайдер, проигнорированные провайдеры,
/// надбавки и признак упора в предел. Нужен UI, чтобы игрок видел, почему число именно такое.
/// </param>
/// <param name="RangedDefenseBreakdown">То же для дальней защиты.</param>
public record DerivedStats(
    int WoundThreshold,
    int StrainThreshold,
    int Soak,
    int MeleeDefense,
    int RangedDefense,
    int EncumbranceThreshold,
    int EncumbranceLoad,
    bool Encumbered,
    DefenseBreakdown? MeleeDefenseBreakdown = null,
    DefenseBreakdown? RangedDefenseBreakdown = null,
    /// <summary>Точное состояние перегруза: помехи, бесплатный манёвр и цена манёвра (ROT-EQP-01).</summary>
    EncumbranceState? Encumbrance = null);
