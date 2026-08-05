using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Что трата символов сделала с результатом: числа экземпляра и строки описания.</summary>
/// <param name="Quantity">Сколько экземпляров или доз создаётся при успехе.</param>
/// <param name="Qualities">Качества, добавленные изготовлением, кодами с рейтингом.</param>
/// <param name="Notes">Все выбранные траты словами — они попадают в описание предмета.</param>
public sealed record CraftingOutcome(
    int Time,
    int Quantity,
    int EncumbranceDelta,
    int HardPointsDelta,
    IReadOnlyList<EffectiveQuality> Qualities,
    bool Fragile,
    IReadOnlyList<string> Notes);

/// <summary>Одна выбранная трата на входе разрешения проекта.</summary>
/// <param name="PaidWith">Каким символом оплачена: <c>advantage</c>, <c>threat</c>, <c>triumph</c>, <c>despair</c>.</param>
public sealed record CraftingSpendChoice(string Code, int Count, string Parameter, string PaidWith);

/// <summary>
/// Правила изготовления, варки и зачарования (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01):
/// сложность, время, стоимость компонентов и распределение символов.
/// </summary>
/// <remarks>
/// Бросок делает клиент и присылает символы — та же конвенция, что у продажи по проверке
/// (ROT-ECO-01) и у разрешения атаки. Сервер не верит присланному итогу: сложность, время,
/// стоимость и каждый эффект траты считаются здесь по кодам таблицы.
/// </remarks>
public static class CraftingRules
{
    /// <summary>
    /// Ровно двенадцать алхимических расходников ROT-ALCH-01. Код — структурный признак рецепта:
    /// обычные consumable (паёк, факелы, лечебные травы) варить навыком Alchemy нельзя.
    /// </summary>
    public static readonly IReadOnlySet<string> PotionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "acid-flask",
        "bottled-courage",
        "health-elixir",
        "immunity-elixir",
        "invisibility-potion",
        "poison",
        "power-potion",
        "protective-tonic",
        "regeneration-elixir",
        "smokebomb-vial",
        "speed-potion",
        "stamina-elixir",
    };

    /// <summary>Запись является рецептом алхимического расходника, включая namespaced code.</summary>
    public static bool IsPotion(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var index = code.LastIndexOf('.');
        return PotionCodes.Contains(index < 0 ? code : code[(index + 1)..]);
    }

    /// <summary>Качества, рейтинг которых поднимать нельзя: так сказано в строке 1 триумфа.</summary>
    public static readonly string[] RatingForbiddenFields = ["damage", "critical", "soak", "defense"];

    private static readonly string[] ForbiddenRatingQualities =
        ["pierce-armor", "breach"];

    /// <summary>Сложность проверки: <c>ceil(rarity / 2)</c>, где 0 — Простая.</summary>
    public static int Difficulty(int rarity) => (Math.Max(0, rarity) + 1) / 2;

    /// <summary>Базовое время: <c>1 + rarity</c> — дни у предмета, часы у одной партии зелья.</summary>
    public static int BaseTime(int rarity) => 1 + Math.Max(0, rarity);

    /// <summary>
    /// Стоимость компонентов: половина цены результата с округлением **вверх** — это
    /// зафиксированное продуктовое решение ТЗ, а не обычное округление вниз, как у выручки.
    /// </summary>
    public static int ComponentCost(int price) => (Math.Max(0, price) + 1) / 2;

    /// <summary>
    /// Итоговая стоимость компонентов. Доля и своя цена — взаимоисключающие способы, ровно как при
    /// покупке (ROT-ECO-01): доля считается от расчётной стоимости и округляется вниз, своя цена
    /// её заменяет и требует причины.
    /// </summary>
    public static int Cost(int listedCost, int percent, int? costOverride, string? overrideReason)
    {
        if (costOverride is not null)
        {
            if (string.IsNullOrWhiteSpace(overrideReason))
                throw new DomainRuleException(
                    "Для своей цены компонентов нужна причина.", "crafting.cost_reason_required");
            if (costOverride < 0)
                throw new DomainRuleException(
                    "Цена не может быть отрицательной.", "trade.price_negative");
            return costOverride.Value;
        }
        if (percent < TradeRules.MinPurchasePercent || percent > TradeRules.MaxPurchasePercent)
            throw new DomainRuleException(
                $"Доля стоимости задаётся от {TradeRules.MinPurchasePercent} до {TradeRules.MaxPurchasePercent} процентов.",
                "trade.percent_invalid");
        if (percent % TradeRules.PurchasePercentStep != 0)
            throw new DomainRuleException(
                $"Доля стоимости задаётся с шагом {TradeRules.PurchasePercentStep} процентов.",
                "trade.percent_step_invalid");
        return (int)Math.Floor(listedCost * (percent / 100.0));
    }

    /// <summary>
    /// Изготовить можно не всё. Уникальная реликвия и любая запись без цены обычным процессом не
    /// создаются: цена компонентов у них не определена, а копия именной вещи — не изготовление
    /// (ROT-CRAFT-MAGIC-01 тоже её не клонирует).
    /// </summary>
    public static void EnsureCraftable(ItemDef def, CraftingKind kind)
    {
        if (kind == CraftingKind.Enchantment) return; // основу не создают, её улучшают
        if (kind == CraftingKind.Potion && !IsPotion(def.Code))
            throw new DomainRuleException(
                "Эта запись не является алхимическим расходником.",
                "crafting.target_not_potion");
        if (kind == CraftingKind.Item && IsPotion(def.Code))
            throw new DomainRuleException(
                "Алхимический расходник нужно создавать через варку зелий.",
                "crafting.target_is_potion");
        if (kind == CraftingKind.Item && ShopCatalogRules.IsService(def.Code))
            throw new DomainRuleException(
                "Услуги, готовую еду и напитки нельзя создавать обычным ремеслом.",
                "crafting.target_not_craftable");
        if (def.Price is null)
            throw new DomainRuleException(
                "У этой записи нет цены — изготовить её обычным процессом нельзя.",
                "crafting.target_priceless");
        if (def.Retired)
            throw new DomainRuleException(
                "Эта запись больше не доступна в системе.", "crafting.target_retired");
    }

    /// <summary>
    /// Зачарование начинается только с подходящей основы: по правилу она уже должна быть
    /// превосходной работы. Проверяется по качествам экземпляра, а не по названию.
    /// </summary>
    public static void EnsureEnchantable(IEnumerable<EffectiveQuality> qualities)
    {
        if (!qualities.Any(q => string.Equals(q.Code, "superior", StringComparison.OrdinalIgnoreCase)))
            throw new DomainRuleException(
                "Зачаровать можно только основу с качеством «Превосходное».",
                "crafting.base_not_superior");
    }

    /// <summary>
    /// Разбирает выбранные траты: проверяет бюджет символов, повторяемость, параметры и
    /// взаимоисключения внутри строки таблицы, затем считает итог.
    /// </summary>
    /// <param name="baseTime">Время проекта до трат.</param>
    /// <param name="isWeapon">Цель — оружие: от этого зависит доступность Неточного.</param>
    public static CraftingOutcome Allocate(
        IReadOnlyList<CraftingSpendChoice> choices,
        IReadOnlyDictionary<string, CraftingSpendDef> catalog,
        int advantages, int threats, int triumphs, int despairs,
        int baseTime, bool isWeapon, bool success)
    {
        var budget = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["advantage"] = advantages, ["threat"] = threats,
            ["triumph"] = triumphs, ["despair"] = despairs,
        };

        var time = baseTime;
        var quantity = success ? 1 : 0;
        var enc = 0;
        var hardPoints = 0;
        var fragile = false;
        var qualities = new List<EffectiveQuality>();
        var notes = new List<string>();
        var usedRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var choice in choices)
        {
            if (!catalog.TryGetValue(choice.Code, out var def))
                throw new DomainRuleException(
                    $"Неизвестная трата «{choice.Code}».", "crafting.spend_unknown");
            var count = choice.Count;
            if (count < 1)
                throw new DomainRuleException(
                    "Трата выбирается хотя бы один раз.", "crafting.spend_count_invalid");
            if (count > 1 && !def.Repeatable)
                throw new DomainRuleException(
                    $"Трату «{def.NameRu}» нельзя выбрать больше одного раза.",
                    "crafting.spend_not_repeatable");
            // Внутри строки таблицы эффекты взаимоисключающие: за одну цену берут один из них.
            if (!usedRows.Add(def.RowCode) && !def.Repeatable)
                throw new DomainRuleException(
                    $"Из строки «{def.RowCode}» уже выбран другой эффект.",
                    "crafting.spend_row_conflict");
            if (def.WeaponOnly && !isWeapon)
                throw new DomainRuleException(
                    $"Трата «{def.NameRu}» применима только к оружию.", "crafting.spend_weapon_only");
            if (def.RequiresParameter && string.IsNullOrWhiteSpace(choice.Parameter))
                throw new DomainRuleException(
                    $"Трате «{def.NameRu}» нужен выбор.", "crafting.spend_parameter_required");
            if (def.Effect == CraftingSpendEffect.QualityRating
                && ForbiddenRatingQualities.Contains(choice.Parameter, StringComparer.OrdinalIgnoreCase))
                throw new DomainRuleException(
                    "Урон, крит, поглощение и защиту этой тратой не поднимают.",
                    "crafting.spend_rating_forbidden");

            var unit = UnitCost(def, choice.PaidWith)
                ?? throw new DomainRuleException(
                    $"Трата «{def.NameRu}» так не оплачивается.", "crafting.spend_payment_invalid");
            var total = unit * count;
            if (budget[choice.PaidWith] < total)
                throw new DomainRuleException(
                    $"Не хватает символов на «{def.NameRu}»: нужно {total}, осталось {budget[choice.PaidWith]}.",
                    "crafting.spend_budget");
            budget[choice.PaidWith] -= total;

            switch (def.Effect)
            {
                case CraftingSpendEffect.Time:
                    time = Math.Max(1, time + def.Value * count);
                    break;
                case CraftingSpendEffect.TimeHalved:
                    for (var i = 0; i < count; i++) time = Math.Max(1, (time + 1) / 2);
                    break;
                case CraftingSpendEffect.Encumbrance:
                    enc += def.Value * count;
                    break;
                case CraftingSpendEffect.HardPoints:
                    hardPoints += def.Value * count;
                    break;
                case CraftingSpendEffect.AddQuality:
                    qualities.Add(new EffectiveQuality(def.Quality, def.Value));
                    break;
                case CraftingSpendEffect.QualityRating:
                    qualities.Add(new EffectiveQuality(choice.Parameter.Trim(), def.Value * count));
                    break;
                case CraftingSpendEffect.ExtraQuantity:
                    if (success) quantity += def.Value * count;
                    break;
                case CraftingSpendEffect.Fragile:
                    fragile = true;
                    break;
            }

            var repeat = count > 1 ? $" ×{count}" : "";
            var parameter = string.IsNullOrWhiteSpace(choice.Parameter) ? "" : $": {choice.Parameter.Trim()}";
            notes.Add($"{def.NameRu}{repeat}{parameter}");
        }

        return new CraftingOutcome(
            time, quantity, enc, hardPoints, MergeQualities(qualities), fragile, notes);
    }

    /// <summary>Цена одной активации выбранным символом; <c>null</c> — этим символом не платят.</summary>
    private static int? UnitCost(CraftingSpendDef def, string paidWith) => paidWith?.ToLowerInvariant() switch
    {
        "advantage" => def.AdvantageCost > 0 ? def.AdvantageCost : null,
        "threat" => def.ThreatCost > 0 ? def.ThreatCost : null,
        "triumph" => def.TriumphCost > 0 ? def.TriumphCost : null,
        "despair" => def.DespairCost > 0 ? def.DespairCost : null,
        _ => null,
    };

    /// <summary>Один код качества — одна строка: два выбора одного рейтинга складываются.</summary>
    private static List<EffectiveQuality> MergeQualities(IEnumerable<EffectiveQuality> qualities) =>
        [.. qualities
            .Where(q => !string.IsNullOrWhiteSpace(q.Code))
            .GroupBy(q => q.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new EffectiveQuality(g.Key, g.Sum(q => q.Rating)))];

    /// <summary>Качества экземпляра, добавленные изготовлением, в компактной строке хранения.</summary>
    public static string PackQualities(IEnumerable<EffectiveQuality> qualities) =>
        string.Join(",", qualities.Select(q => q.Rating > 0 ? $"{q.Code}:{q.Rating}" : q.Code));

    /// <summary>Обратный разбор строки хранения. Мусор молча пропускается, а не считается качеством.</summary>
    public static List<EffectiveQuality> UnpackQualities(string packed)
    {
        var result = new List<EffectiveQuality>();
        foreach (var part in (packed ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split(':', 2);
            var code = pieces[0].Trim();
            if (code.Length == 0) continue;
            var rating = pieces.Length > 1 && int.TryParse(pieces[1], out var r) ? r : 0;
            result.Add(new EffectiveQuality(code, rating));
        }
        return result;
    }
}
