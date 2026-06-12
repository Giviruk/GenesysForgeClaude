namespace GenesysForge.Application.Dtos;

public record DerivedDto(int WoundThreshold, int StrainThreshold, int Soak, int MeleeDefense, int RangedDefense,
    int EncumbranceThreshold, int EncumbranceLoad, bool Encumbered);
