namespace GenesysForge.Domain;

public record ItemInput(
    string Name,
    ItemKind Kind,
    ItemState State,
    int Encumbrance,
    int Quantity = 1,
    int SoakBonus = 0,
    int MeleeDefense = 0,
    int RangedDefense = 0,
    int EncumbranceThresholdBonus = 0);
