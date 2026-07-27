namespace GenesysForge.Domain;

/// <summary>Качество предмета в расчётах листа: код справочника и рейтинг (0 — качество без рейтинга).</summary>
public record ItemQualityInput(string Code, int Rating = 0);

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
    bool IsActiveArmor = false,
    /// <summary>
    /// Структурные качества предмета. Из них берутся Defensive/Deflection: щит — оружие, а не
    /// броня, и его защита складывается с бронёй, а не конкурирует с ней (ROT-WPN-01).
    /// </summary>
    IReadOnlyList<ItemQualityInput>? Qualities = null,
    /// <summary>
    /// Оружие метнули и не подобрали: оно лежит у цели, поэтому ни качеств, ни веса не даёт
    /// (ROT-WPN-01).
    /// </summary>
    bool IsThrown = false);
