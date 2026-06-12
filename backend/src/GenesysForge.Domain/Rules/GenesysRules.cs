namespace GenesysForge.Domain;

/// <summary>
/// Чистые правила Genesys, общие для Genesys Core и Realms of Terrinoth.
/// Все формулы — из Genesys Core Rulebook.
/// </summary>
public static class GenesysRules
{
    public const int MinCharacteristic = 1;
    public const int MaxCharacteristicAtCreation = 5;
    public const int MaxCharacteristic = 6;
    public const int MaxSkillRank = 5;
    public const int MaxSkillRankAtCreation = 2;
    public const int MaxTalentTier = 5;

    /// <summary>
    /// Дайс-пул: берётся большее из (характеристика, ранги навыка) кубов,
    /// из них min(характеристика, ранги) улучшаются до Proficiency.
    /// </summary>
    public static DicePool BuildDicePool(int characteristic, int ranks)
    {
        if (characteristic < 0) throw new ArgumentOutOfRangeException(nameof(characteristic));
        if (ranks < 0) throw new ArgumentOutOfRangeException(nameof(ranks));
        var proficiency = Math.Min(characteristic, ranks);
        var ability = Math.Max(characteristic, ranks) - proficiency;
        return new DicePool(ability, proficiency);
    }

    /// <summary>Стоимость повышения характеристики до нового значения (только при создании): 10 × новое значение.</summary>
    public static int CharacteristicUpgradeCost(int newValue) => newValue * 10;

    /// <summary>Стоимость нового ранга навыка: 5 × новый ранг, +5 если навык некарьерный.</summary>
    public static int SkillRankCost(int newRank, bool isCareerSkill) =>
        newRank * 5 + (isCareerSkill ? 0 : 5);

    /// <summary>Стоимость таланта: 5 × тир.</summary>
    public static int TalentCost(int tier) => tier * 5;

    /// <summary>
    /// Эффективный тир для очередной покупки рангового таланта:
    /// каждый следующий ранг покупается как талант на тир выше (максимум 5).
    /// </summary>
    /// <param name="baseTier">Базовый тир таланта.</param>
    /// <param name="ranksAlreadyOwned">Сколько рангов уже куплено.</param>
    public static int RankedTalentEffectiveTier(int baseTier, int ranksAlreadyOwned)
    {
        if (baseTier is < 1 or > MaxTalentTier) throw new ArgumentOutOfRangeException(nameof(baseTier));
        if (ranksAlreadyOwned < 0) throw new ArgumentOutOfRangeException(nameof(ranksAlreadyOwned));
        return Math.Min(baseTier + ranksAlreadyOwned, MaxTalentTier);
    }

    /// <summary>
    /// Правило пирамиды талантов: после покупки таланта тира N персонаж обязан иметь
    /// талантов каждого более низкого тира строго больше, чем талантов следующего тира.
    /// </summary>
    /// <param name="tierCounts">Количество уже имеющихся талантов по тирам, индексы 1..5.</param>
    /// <param name="tierToBuy">Тир покупаемого таланта (эффективный, для ранговых).</param>
    public static bool CanPurchaseTalentTier(IReadOnlyDictionary<int, int> tierCounts, int tierToBuy)
    {
        if (tierToBuy is < 1 or > MaxTalentTier) return false;

        var counts = new int[MaxTalentTier + 2];
        for (var t = 1; t <= MaxTalentTier; t++)
            counts[t] = tierCounts.TryGetValue(t, out var c) ? c : 0;
        counts[tierToBuy]++;

        for (var t = 2; t <= MaxTalentTier; t++)
        {
            if (counts[t] > 0 && counts[t - 1] < counts[t] + 1)
                return false;
        }
        return true;
    }

    /// <summary>Порог ран (HP): база архетипа + Brawn + модификаторы.</summary>
    public static int WoundThreshold(int archetypeBase, int brawn, int modifiers = 0) =>
        archetypeBase + brawn + modifiers;

    /// <summary>Порог стрейна (стамина): база архетипа + Willpower + модификаторы.</summary>
    public static int StrainThreshold(int archetypeBase, int willpower, int modifiers = 0) =>
        archetypeBase + willpower + modifiers;

    /// <summary>Поглощение: Brawn + поглощение надетой брони + модификаторы.</summary>
    public static int Soak(int brawn, int equippedArmorSoak, int modifiers = 0) =>
        brawn + equippedArmorSoak + modifiers;

    /// <summary>Порог переносимого веса: 5 + Brawn + бонусы предметов (например, рюкзак).</summary>
    public static int EncumbranceThreshold(int brawn, int itemBonuses = 0) =>
        5 + brawn + itemBonuses;

    /// <summary>Надетая броня весит на 3 меньше (минимум 0).</summary>
    public static int WornArmorEncumbrance(int armorEncumbrance) =>
        Math.Max(0, armorEncumbrance - 3);
}
