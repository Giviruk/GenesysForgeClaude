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
    int EncumbranceThresholdBonus = 0,
    /// <summary>
    /// Позиция — выбранная активная броня (ROT-CMB-02). Защиту и поглощение даёт только она;
    /// прочая надетая броня продолжает считаться в переносимый вес, но защиты не даёт.
    /// </summary>
    bool IsActiveArmor = false);
