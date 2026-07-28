namespace GenesysForge.Domain.Rules;

/// <summary>
/// Как инструмент удешевляет дополнительный эффект заклинания (ROT-MAG-IMP-01).
/// </summary>
public enum ImplementDiscountKind
{
    /// <summary>Инструмент ничего не удешевляет.</summary>
    None = 0,

    /// <summary>Названные эффекты бесплатны: скипетр — Ближний бой, инструмент — Доп. цель.</summary>
    NamedEffects = 1,

    /// <summary>
    /// Бесплатно только первое добавление любого из названных эффектов: посох — первая Дистанция.
    /// Отличается от <see cref="NamedEffects"/> тем, что скидка одна на весь список, а не своя
    /// у каждого эффекта.
    /// </summary>
    FirstNamedEffect = 2,

    /// <summary>Каждый эффект, доступный только одному навыку, дешевле на единицу: икона — Вера.</summary>
    RestrictedSkillDiscount = 3,

    /// <summary>Бесплатны эффекты, выбранные при изготовлении экземпляра: фолиант и палочка.</summary>
    ChosenEffects = 4,
}

/// <summary>
/// Паспорт магического инструмента (ROT-MAG-IMP-01). Числа — из таблицы книги; правило —
/// типизированное, а не разбор описания.
/// </summary>
/// <param name="Code">Код записи каталога без префикса системы.</param>
/// <param name="AttackDamageBonus">
/// Прибавка к базовому урону магической Атаки. Это не урон ближнего боя и на Лечение и Проклятье
/// он не влияет.
/// </param>
/// <param name="BoostDice">Бонусные кости к магической проверке: скипетр даёт одну.</param>
/// <param name="RequiredMagicSkill">
/// Инструмент работает только с этим навыком; пусто — с любым. Музыкальный инструмент — только Песнь.
/// </param>
/// <param name="DiscountEffects">Коды эффектов, которых касается скидка.</param>
/// <param name="ChoiceCount">Сколько эффектов выбирается при изготовлении экземпляра.</param>
/// <param name="ChoiceMaxIncreaseSum">
/// Рекомендованный потолок суммы обычных надбавок выбранных эффектов; <c>null</c> — потолка нет.
/// </param>
/// <param name="ChoiceExactIncrease">
/// Надбавка, которую обязан иметь выбранный эффект; <c>null</c> — любая. Палочка берёт ровно +1.
/// </param>
public sealed record ImplementSpec(
    string Code,
    int AttackDamageBonus,
    int Encumbrance,
    int Price,
    int Rarity,
    ImplementDiscountKind Discount,
    IReadOnlyList<string> DiscountEffects,
    int BoostDice = 0,
    string RequiredMagicSkill = "",
    int ChoiceCount = 0,
    int? ChoiceMaxIncreaseSum = null,
    int? ChoiceExactIncrease = null)
{
    /// <summary>Экземпляр настраивается ведущим при изготовлении: фолиант и палочка.</summary>
    public bool NeedsConfiguration => ChoiceCount > 0;
}

/// <summary>Дополнительный эффект заклинания на входе расчёта сложности.</summary>
/// <param name="Code">Английское имя-код эффекта: <c>Range</c>, <c>Close Combat</c>…</param>
/// <param name="Increase">Печатная надбавка к сложности: 1 или 2.</param>
/// <param name="RestrictedSkill">
/// Навык, которому эффект доступен исключительно; пусто — доступен нескольким. Именно на такие
/// эффекты действует скидка иконы.
/// </param>
public sealed record SpellEffectInput(string Code, int Increase, string RestrictedSkill = "");

/// <summary>Скидка инструмента на один эффект — чтобы в UI было видно, почему сложность ниже.</summary>
public sealed record ImplementDiscount(string EffectCode, int Reduction, string Reason);

/// <summary>
/// Итог расчёта сложности с инструментом. Сложность никогда не опускается ниже базовой сложности
/// самого действия: инструмент удешевляет добавки, а не само заклинание.
/// </summary>
public sealed record SpellDifficulty(
    int BaseDifficulty,
    int Raw,
    int Effective,
    int BoostDice,
    IReadOnlyList<ImplementDiscount> Discounts)
{
    public int TotalReduction => Raw - Effective;
}

/// <summary>
/// Магические инструменты (ROT-MAG-IMP-01) и их материалы (ROT-MAG-MAT-01).
///
/// На одну магическую проверку работает ровно один инструмент, и он должен быть в руках:
/// посох с палочкой не складываются, а лежащий в рюкзаке фолиант не помогает. Отбор «какой именно»
/// делает вызывающий — это правило про инвентарь; здесь живут числа и скидки.
/// </summary>
public static class ImplementRules
{
    /// <summary>Пределы редкости — те же, что у качества изготовления.</summary>
    public const int MinRarity = CraftsmanshipRules.MinRarity;
    public const int MaxRarity = CraftsmanshipRules.MaxRarity;

    private const string RangeEffect = "Range";
    private const string CloseCombatEffect = "Close Combat";
    private const string AdditionalTargetEffect = "Additional Target";

    /// <summary>Навык, которому доступна икона: скидка считается по эффектам только для Веры.</summary>
    public const string DivineSkill = "Divine";

    /// <summary>Навык музыкального инструмента.</summary>
    public const string VerseSkill = "Verse";

    /// <summary>Шесть записей таблицы книги. Ключ — код каталога без префикса системы.</summary>
    private static readonly Dictionary<string, ImplementSpec> ByCode = new(StringComparer.Ordinal)
    {
        ["holy-icon"] = new("holy-icon", AttackDamageBonus: 0, Encumbrance: 0, Price: 250, Rarity: 4,
            ImplementDiscountKind.RestrictedSkillDiscount, [], RequiredMagicSkill: DivineSkill),
        ["magic-scepter"] = new("magic-scepter", 2, 1, 350, 5,
            ImplementDiscountKind.NamedEffects, [CloseCombatEffect], BoostDice: 1),
        ["magic-staff"] = new("magic-staff", 4, 2, 400, 6,
            ImplementDiscountKind.FirstNamedEffect, [RangeEffect]),
        ["magic-tome"] = new("magic-tome", 0, 1, 750, 7,
            ImplementDiscountKind.ChosenEffects, [], ChoiceCount: 2, ChoiceMaxIncreaseSum: 3),
        ["magic-wand"] = new("magic-wand", 3, 1, 400, 7,
            ImplementDiscountKind.ChosenEffects, [], ChoiceCount: 1, ChoiceExactIncrease: 1),
        ["musical-instrument"] = new("musical-instrument", 0, 1, 200, 4,
            ImplementDiscountKind.NamedEffects, [AdditionalTargetEffect], RequiredMagicSkill: VerseSkill),
    };

    /// <summary>Все инструменты в порядке таблицы — для справочника и тестов.</summary>
    public static IReadOnlyList<ImplementSpec> All { get; } = [.. ByCode.Values];

    /// <summary>Материалы в порядке таблицы.</summary>
    public static IReadOnlyList<ImplementMaterial> Materials { get; } =
        [ImplementMaterial.Bone, ImplementMaterial.Oak, ImplementMaterial.Hazel,
         ImplementMaterial.Willow, ImplementMaterial.Yew];

    /// <summary>
    /// Паспорт инструмента по коду каталога, или <c>null</c>, если запись инструментом не является.
    /// Код приходит с префиксом системы (<c>rot.item.magic-staff</c>) — сравнивается хвост.
    /// </summary>
    public static ImplementSpec? For(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        var bare = code[(code.LastIndexOf('.') + 1)..];
        return ByCode.TryGetValue(bare, out var spec) ? spec : null;
    }

    /// <summary>Запись каталога — магический инструмент.</summary>
    public static bool IsImplement(string? code) => For(code) is not null;

    /// <summary>
    /// Множитель цены материала. У кости, орешника и тиса — полтора по официальной errata;
    /// печатное «вдвое дешевле» не используется, иначе редкий материал выходил бы дешевле дуба.
    /// </summary>
    public static decimal PriceMultiplier(ImplementMaterial material) => material switch
    {
        ImplementMaterial.Bone or ImplementMaterial.Hazel or ImplementMaterial.Yew => 1.5m,
        ImplementMaterial.Willow => 2m,
        _ => 1m,
    };

    /// <summary>Сдвиг редкости материала.</summary>
    public static int RarityShift(ImplementMaterial material) => material switch
    {
        ImplementMaterial.Bone or ImplementMaterial.Willow => 2,
        ImplementMaterial.Hazel or ImplementMaterial.Yew => 1,
        _ => 0,
    };

    /// <summary>
    /// Цена экземпляра. Дробь округляется вверх до целой монеты — видимое продуктовое решение:
    /// у полуторного множителя нечётная цена иначе давала бы половину монеты.
    /// </summary>
    public static int Price(int basePrice, ImplementMaterial material) =>
        (int)Math.Ceiling(basePrice * PriceMultiplier(material));

    /// <summary>Редкость экземпляра, обрезанная диапазоном 0…10.</summary>
    public static int Rarity(int baseRarity, ImplementMaterial material) =>
        Math.Clamp(baseRarity + RarityShift(material), MinRarity, MaxRarity);

    /// <summary>Материал бывает только у инструмента: у мешка и меча его не бывает.</summary>
    public static void EnsureApplicable(string? code, ImplementMaterial material)
    {
        if (!Enum.IsDefined(material))
            throw new DomainRuleException("Неизвестный материал.", "implement.material.unknown");
        if (material != ImplementMaterial.Oak && !IsImplement(code))
            throw new DomainRuleException(
                "Материал бывает только у магического инструмента.", "implement.material.not_applicable");
    }

    /// <summary>
    /// Проверяет выбор эффектов, который ведущий задаёт экземпляру при изготовлении. Выбор делается
    /// один раз и дальше неизменен, поэтому проверяется здесь и только здесь.
    /// </summary>
    /// <param name="chosen">Выбранные эффекты; их надбавки нужны для проверки бюджета.</param>
    /// <param name="overrideReason">
    /// Причина превышения рекомендованного бюджета фолианта. Книга формулирует его как совет
    /// ведущему, поэтому это не запрет, а явное решение с причиной.
    /// </param>
    public static void EnsureConfigurationValid(
        ImplementSpec spec, IReadOnlyList<SpellEffectInput> chosen, string? overrideReason = null)
    {
        if (!spec.NeedsConfiguration)
            throw new DomainRuleException(
                "Этот инструмент не настраивается.", "implement.configuration.not_applicable");
        if (chosen.Count > spec.ChoiceCount)
            throw new DomainRuleException(
                $"Можно выбрать не больше {spec.ChoiceCount} эффектов.", "implement.choices.too_many");
        if (chosen.Select(c => c.Code).Distinct(StringComparer.Ordinal).Count() != chosen.Count)
            throw new DomainRuleException(
                "Эффекты в выборе не повторяются.", "implement.choices.duplicate");

        // Палочка берёт эффект строго с печатной надбавкой +1: иначе выбором можно было бы
        // бесплатно получить то, что стоит дороже.
        if (spec.ChoiceExactIncrease is { } exact && chosen.Any(c => c.Increase != exact))
            throw new DomainRuleException(
                $"Этот инструмент берёт только эффект с надбавкой +{exact}.",
                "implement.choices.increase_mismatch");

        if (spec.ChoiceMaxIncreaseSum is { } max
            && chosen.Sum(c => c.Increase) > max
            && string.IsNullOrWhiteSpace(overrideReason))
            throw new DomainRuleException(
                $"Обычно сумма надбавок выбранных эффектов не выше {max}; "
                + "превышение — решение ведущего, и ему нужна причина.",
                "implement.choices.budget_exceeded");
    }

    /// <summary>
    /// Считает сложность магического действия с учётом инструмента. Порядок фиксирован: сначала
    /// сложность собирается из действия и добавленных эффектов, потом инструмент удешевляет
    /// конкретные добавки, и итог не опускается ниже базовой сложности действия.
    /// </summary>
    /// <param name="spec">Инструмент в руках; <c>null</c> — считается без него.</param>
    /// <param name="magicSkill">Навык проверки: инструмент своего навыка работает только с ним.</param>
    /// <param name="configured">Эффекты, выбранные у экземпляра фолианта или палочки.</param>
    /// <param name="pending">
    /// Экземпляр ещё не настроен ведущим: обычные числа у него есть, а бесплатный эффект не работает.
    /// </param>
    public static SpellDifficulty Difficulty(
        int baseDifficulty,
        IReadOnlyList<SpellEffectInput> effects,
        ImplementSpec? spec = null,
        string magicSkill = "",
        IReadOnlyList<string>? configured = null,
        bool pending = false)
    {
        var raw = baseDifficulty + effects.Sum(e => Math.Max(0, e.Increase));
        var discounts = new List<ImplementDiscount>();

        if (spec is not null && Works(spec, magicSkill) && !pending)
        {
            var firstUsed = false;
            // Скидка даётся один раз на эффект: повторяемую Дистанцию инструмент удешевляет
            // только в первом добавлении, второе и третье стоят полную надбавку.
            var discounted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var effect in effects)
            {
                if (!discounted.Add(effect.Code)
                    && spec.Discount is ImplementDiscountKind.NamedEffects
                        or ImplementDiscountKind.ChosenEffects
                        or ImplementDiscountKind.RestrictedSkillDiscount)
                    continue;
                var reduction = spec.Discount switch
                {
                    ImplementDiscountKind.NamedEffects when Named(spec, effect) => effect.Increase,
                    ImplementDiscountKind.FirstNamedEffect when Named(spec, effect) && !firstUsed =>
                        effect.Increase,
                    // Икона удешевляет, но не обнуляет: эффект за +2 всё ещё стоит +1.
                    ImplementDiscountKind.RestrictedSkillDiscount
                        when string.Equals(effect.RestrictedSkill, spec.RequiredMagicSkill,
                            StringComparison.OrdinalIgnoreCase) => Math.Min(1, effect.Increase),
                    ImplementDiscountKind.ChosenEffects
                        when configured?.Contains(effect.Code, StringComparer.Ordinal) == true =>
                        effect.Increase,
                    _ => 0,
                };
                if (reduction <= 0) continue;
                if (spec.Discount == ImplementDiscountKind.FirstNamedEffect) firstUsed = true;
                discounts.Add(new ImplementDiscount(effect.Code, reduction, spec.Code));
            }
        }

        var effective = Math.Max(baseDifficulty, raw - discounts.Sum(d => d.Reduction));
        var boost = spec is not null && Works(spec, magicSkill) ? spec.BoostDice : 0;
        return new SpellDifficulty(baseDifficulty, raw, effective, boost, discounts);
    }

    /// <summary>Инструмент действует для этой проверки: у него либо нет своего навыка, либо он совпал.</summary>
    public static bool Works(ImplementSpec spec, string magicSkill) =>
        string.IsNullOrEmpty(spec.RequiredMagicSkill)
        || string.Equals(spec.RequiredMagicSkill, magicSkill, StringComparison.OrdinalIgnoreCase);

    private static bool Named(ImplementSpec spec, SpellEffectInput effect) =>
        spec.DiscountEffects.Contains(effect.Code, StringComparer.Ordinal);
}
