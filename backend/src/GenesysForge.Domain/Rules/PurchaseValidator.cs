namespace GenesysForge.Domain;

/// <summary>Валидация покупок за XP по правилам Genesys.</summary>
public static class PurchaseValidator
{
    public static PurchaseResult BuySkillRank(int currentRank, bool isCareer, int availableXp, bool isCreationPhase)
    {
        var newRank = currentRank + 1;
        if (newRank > GenesysRules.MaxSkillRank)
            return PurchaseResult.Fail($"Максимальный ранг навыка — {GenesysRules.MaxSkillRank}.");
        if (isCreationPhase && newRank > GenesysRules.MaxSkillRankAtCreation)
            return PurchaseResult.Fail($"При создании персонажа ранг навыка не может превышать {GenesysRules.MaxSkillRankAtCreation}.");
        var cost = GenesysRules.SkillRankCost(newRank, isCareer);
        return availableXp < cost
            ? PurchaseResult.Fail($"Недостаточно XP: нужно {cost}, доступно {availableXp}.")
            : PurchaseResult.Ok(cost);
    }

    public static PurchaseResult BuyCharacteristic(int currentValue, int availableXp, bool isCreationPhase)
    {
        if (!isCreationPhase)
            return PurchaseResult.Fail("Характеристики можно повышать за XP только при создании персонажа (далее — талантом Dedication).");
        var newValue = currentValue + 1;
        if (newValue > GenesysRules.MaxCharacteristicAtCreation)
            return PurchaseResult.Fail($"При создании характеристика не может превышать {GenesysRules.MaxCharacteristicAtCreation}.");
        var cost = GenesysRules.CharacteristicUpgradeCost(newValue);
        return availableXp < cost
            ? PurchaseResult.Fail($"Недостаточно XP: нужно {cost}, доступно {availableXp}.")
            : PurchaseResult.Ok(cost);
    }

    /// <summary>Возврат последнего повышения характеристики (только при создании, не ниже базы архетипа).</summary>
    public static PurchaseResult RefundCharacteristic(int currentValue, int archetypeBase, bool isCreationPhase)
    {
        if (!isCreationPhase)
            return PurchaseResult.Fail("Откат покупок доступен только до завершения создания персонажа.");
        if (currentValue <= archetypeBase)
            return PurchaseResult.Fail("Характеристика уже на стартовом значении архетипа.");
        // возвращается то, что было заплачено за текущее значение
        return PurchaseResult.Ok(GenesysRules.CharacteristicUpgradeCost(currentValue));
    }

    /// <summary>Возврат последнего ранга навыка (только при создании; бесплатные стартовые ранги не возвращаются).</summary>
    public static PurchaseResult RefundSkillRank(int currentRank, int freeRanks, bool isCareer, bool isCreationPhase)
    {
        if (!isCreationPhase)
            return PurchaseResult.Fail("Откат покупок доступен только до завершения создания персонажа.");
        if (currentRank <= 0)
            return PurchaseResult.Fail("У навыка нет рангов для возврата.");
        if (currentRank <= freeRanks)
            return PurchaseResult.Fail("Бесплатный стартовый ранг карьерного навыка вернуть нельзя.");
        return PurchaseResult.Ok(GenesysRules.SkillRankCost(currentRank, isCareer));
    }

    /// <summary>
    /// Возврат последнего ранга таланта (только при создании).
    /// Удаление не должно ломать пирамиду для оставшихся талантов.
    /// </summary>
    public static PurchaseResult RefundTalent(
        int baseTier,
        int ranksOwned,
        IReadOnlyDictionary<int, int> tierCounts,
        bool isCreationPhase)
    {
        if (!isCreationPhase)
            return PurchaseResult.Fail("Откат покупок доступен только до завершения создания персонажа.");
        if (ranksOwned < 1)
            return PurchaseResult.Fail("Этот талант не куплен.");

        var lastTier = GenesysRules.RankedTalentEffectiveTier(baseTier, ranksOwned - 1);
        if (!GenesysRules.CanRemoveTalentTier(tierCounts, lastTier))
            return PurchaseResult.Fail(
                "Нельзя вернуть талант: нарушится пирамида. Сначала верните таланты более высоких тиров.");

        return PurchaseResult.Ok(GenesysRules.TalentCost(lastTier));
    }

    /// <param name="baseTier">Базовый тир покупаемого таланта.</param>
    /// <param name="ranksOwned">Сколько рангов этого таланта уже есть (0, если талант новый).</param>
    /// <param name="isRanked">Является ли талант ранговым.</param>
    /// <param name="tierCounts">Количество талантов персонажа по тирам (каждый ранг считается отдельным талантом своего эффективного тира).</param>
    /// <param name="availableXp">Доступный XP.</param>
    public static PurchaseResult BuyTalent(
        int baseTier,
        int ranksOwned,
        bool isRanked,
        IReadOnlyDictionary<int, int> tierCounts,
        int availableXp)
    {
        if (baseTier is < 1 or > GenesysRules.MaxTalentTier)
            return PurchaseResult.Fail("Тир таланта должен быть от 1 до 5.");
        if (ranksOwned > 0 && !isRanked)
            return PurchaseResult.Fail("Этот талант уже куплен и не является ранговым.");

        var effectiveTier = GenesysRules.RankedTalentEffectiveTier(baseTier, ranksOwned);
        if (!GenesysRules.CanPurchaseTalentTier(tierCounts, effectiveTier))
            return PurchaseResult.Fail(
                $"Нарушено правило пирамиды: для покупки таланта тира {effectiveTier} нужно иметь больше талантов каждого более низкого тира.");

        var cost = GenesysRules.TalentCost(effectiveTier);
        return availableXp < cost
            ? PurchaseResult.Fail($"Недостаточно XP: нужно {cost}, доступно {availableXp}.")
            : PurchaseResult.Ok(cost);
    }
}
