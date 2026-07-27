namespace GenesysForge.Domain.Rules;

/// <summary>Сложность проверки поиска товара по итоговой редкости (ROT-ECO-01).</summary>
/// <param name="Difficulty">Число костей сложности.</param>
/// <param name="Upgrades">
/// Сколько раз GM может улучшить проверку: по одному за каждый пункт редкости сверх 10.
/// Сама сложность выше Formidable не растёт.
/// </param>
public sealed record RarityCheck(int Difficulty, int Upgrades);

/// <summary>Обстоятельства рынка, меняющие итоговую редкость товара.</summary>
public enum MarketCondition
{
    /// <summary>Средний город или обычная цивилизованная территория: без изменений.</summary>
    Ordinary = 0,

    /// <summary>Общество потребления: товар найти проще.</summary>
    ConsumerEconomy = 1,

    /// <summary>Крупная метрополия.</summary>
    MajorMetropolis = 2,

    /// <summary>Торговый узел.</summary>
    TradingHub = 3,

    /// <summary>Сельская местность.</summary>
    Rural = 4,

    /// <summary>Регулируемая экономика — учитывается, только если регулирование касается товара.</summary>
    RegulatedForThisGood = 5,

    /// <summary>Фронтир.</summary>
    Frontier = 6,

    /// <summary>Владение товаром запрещено.</summary>
    ProhibitedOwnership = 7,

    /// <summary>Активная зона боевых действий.</summary>
    ActiveWarZone = 8,

    /// <summary>Пустошь после катастрофы.</summary>
    PostDisasterWasteland = 9,
}

/// <summary>
/// Экономика: поиск товара, цена покупки и выручка от продажи (ROT-ECO-01).
/// Все денежные величины считает сервер; присланные клиентом цена и выручка не используются.
/// </summary>
public static class TradeRules
{
    /// <summary>Наибольшая сложность таблицы — Formidable.</summary>
    public const int MaxDifficulty = 5;

    /// <summary>Редкость, после которой сложность не растёт, а GM получает право на upgrade.</summary>
    private const int RarityUpgradeStart = 10;

    /// <summary>Насколько обстоятельство рынка сдвигает редкость.</summary>
    public static int RarityModifier(MarketCondition condition) => condition switch
    {
        MarketCondition.ConsumerEconomy => -1,
        MarketCondition.MajorMetropolis => -1,
        MarketCondition.TradingHub => -1,
        MarketCondition.Ordinary => 0,
        MarketCondition.Rural => 1,
        MarketCondition.RegulatedForThisGood => 1,
        MarketCondition.Frontier => 2,
        MarketCondition.ProhibitedOwnership => 2,
        MarketCondition.ActiveWarZone => 3,
        MarketCondition.PostDisasterWasteland => 4,
        _ => 0,
    };

    /// <summary>Итоговая редкость с учётом обстоятельств; ниже нуля не опускается.</summary>
    public static int EffectiveRarity(int baseRarity, IEnumerable<MarketCondition>? conditions = null) =>
        Math.Max(0, baseRarity + (conditions ?? []).Sum(RarityModifier));

    /// <summary>
    /// Сложность поиска по итоговой редкости: 0 → Simple, 1–2 → Easy, 3–4 → Average, 5–6 → Hard,
    /// 7–8 → Daunting, 9–10 → Formidable. Свыше 10 сложность остаётся Formidable, а каждый лишний
    /// пункт даёт GM одно улучшение проверки.
    /// </summary>
    public static RarityCheck SearchCheck(int effectiveRarity)
    {
        var rarity = Math.Max(0, effectiveRarity);
        var difficulty = rarity == 0 ? 0 : Math.Min(MaxDifficulty, (rarity + 1) / 2);
        var upgrades = Math.Max(0, rarity - RarityUpgradeStart);
        return new RarityCheck(difficulty, upgrades);
    }

    /// <summary>
    /// Доля от цены при продаже, в процентах: провал не продаёт ничего, 1 успех — 25 %,
    /// 2 — 50 %, 3 и больше — 75 %.
    /// </summary>
    public static int ProceedsPercent(int netSuccesses) => netSuccesses switch
    {
        <= 0 => 0,
        1 => 25,
        2 => 50,
        _ => 75,
    };

    /// <summary>
    /// Выручка по книге до возможной поправки за состояние. Доля берётся от цены **единицы** и
    /// округляется вниз, а потом умножается на количество: округлять вверх каждый процент или
    /// считать долю от присланной клиентом цены нельзя.
    /// </summary>
    public static int BookSubtotal(int unitListedPrice, int quantity, int percent)
    {
        if (unitListedPrice < 0)
            throw new DomainRuleException("Цена предмета не может быть отрицательной.", "trade.price_negative");
        if (quantity < 1)
            throw new DomainRuleException("Количество должно быть не меньше 1.", "trade.quantity_invalid");

        var unitProceeds = (int)Math.Floor(unitListedPrice * (percent / 100.0));
        return unitProceeds * quantity;
    }

    /// <summary>
    /// Итоговая выручка. Скидка за состояние не является автоматическим правилом: множитель
    /// задаётся явно и виден в истории, иначе он равен единице.
    /// </summary>
    public static int FinalProceeds(int bookSubtotal, double conditionMultiplier = 1.0)
    {
        if (conditionMultiplier < 0)
            throw new DomainRuleException(
                "Множитель за состояние не может быть отрицательным.", "trade.condition_multiplier_invalid");
        return Math.Max(0, (int)Math.Floor(bookSubtotal * conditionMultiplier));
    }

    /// <summary>Стоимость покупки: цена по каталогу, умноженная на количество.</summary>
    public static int PurchaseTotal(int unitListedPrice, int quantity)
    {
        if (unitListedPrice < 0)
            throw new DomainRuleException("Цена предмета не может быть отрицательной.", "trade.price_negative");
        if (quantity < 1)
            throw new DomainRuleException("Количество должно быть не меньше 1.", "trade.quantity_invalid");
        return unitListedPrice * quantity;
    }

    /// <summary>Границы и шаг торга при покупке: от половины цены до двойной, четвертями.</summary>
    public const int MinPurchasePercent = 50;
    public const int MaxPurchasePercent = 200;
    public const int PurchasePercentStep = 25;

    /// <summary>
    /// Стоимость покупки с наценкой или скидкой обстановки: доля берётся от цены **единицы**
    /// и округляется вниз, как и при продаже. Правила автоматической скидки не дают, поэтому
    /// доля задаётся явно и попадает в историю.
    /// </summary>
    public static int PurchaseTotal(int unitListedPrice, int quantity, int percent)
    {
        if (percent < MinPurchasePercent || percent > MaxPurchasePercent)
            throw new DomainRuleException(
                $"Доля цены при покупке задаётся от {MinPurchasePercent} до {MaxPurchasePercent} процентов.",
                "trade.percent_invalid");
        if (percent % PurchasePercentStep != 0)
            throw new DomainRuleException(
                $"Доля цены при покупке задаётся с шагом {PurchasePercentStep} процентов.",
                "trade.percent_step_invalid");
        return BookSubtotal(unitListedPrice, quantity, percent);
    }
}
