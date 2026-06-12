namespace GenesysForge.Domain;

public record DerivedStats(
    int WoundThreshold,
    int StrainThreshold,
    int Soak,
    int MeleeDefense,
    int RangedDefense,
    int EncumbranceThreshold,
    int EncumbranceLoad,
    bool Encumbered);
