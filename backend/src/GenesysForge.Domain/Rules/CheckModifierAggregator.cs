namespace GenesysForge.Domain.Rules;

/// <summary>
/// Один вклад в помехи конкретной проверки. Источник называется явно, чтобы игрок видел не просто
/// «+2 помехи», а из-за чего именно они появились.
/// </summary>
/// <param name="SourceType">Тип источника: <c>Item</c>, <c>Encumbrance</c>, <c>CriticalInjury</c>.</param>
/// <param name="SourceName">Английское имя источника (название брони и т. п.).</param>
/// <param name="SourceNameRu">Русское имя источника; пусто — использовать <see cref="SourceName"/>.</param>
/// <param name="Setback">Сколько костей помех добавляет (&gt;0) или убирает (&lt;0) источник.</param>
/// <param name="Condition">
/// Условие из книги, при котором вклад применим. Пусто — применяется всегда; непустое условие
/// приложение не проверяет и в пул автоматически не подставляет.
/// </param>
/// <param name="Boost">Сколько костей умения добавляет источник (улучшения брони, ROT-EQP-ATT-01).</param>
/// <param name="Difficulty">Сколько фиолетовых костей сложности добавляет источник.</param>
/// <param name="DifficultyUpgrades">Сколько раз источник усиливает сложность.</param>
/// <param name="RemoveBoosts">Источник запрещает бонусные кости этого броска.</param>
public sealed record CheckModifierSource(
    string SourceType, string SourceName, string SourceNameRu, int Setback, string Condition = "",
    int Boost = 0, int Difficulty = 0, int DifficultyUpgrades = 0, bool RemoveBoosts = false)
{
    public bool IsConditional => !string.IsNullOrEmpty(Condition);
}

/// <summary>
/// Итог по одной проверке: сколько костей помех добавляется автоматически и из чего они сложились.
/// </summary>
/// <param name="SetbackDice">Безусловные помехи; уже с учётом снятий и не ниже нуля.</param>
/// <param name="Sources">Все вклады, включая условные — их видно в подсказке.</param>
/// <param name="BoostDice">Безусловные кости умения от снаряжения (ROT-EQP-ATT-01).</param>
/// <param name="DifficultyDice">Безусловные кости сложности от критических травм.</param>
/// <param name="DifficultyUpgrades">Безусловные усиления сложности от критических травм.</param>
/// <param name="RemoveBoosts">Нужно ли убрать все бонусные кости из пула.</param>
public sealed record CheckPenalty(
    int SetbackDice, IReadOnlyList<CheckModifierSource> Sources, int BoostDice = 0,
    int DifficultyDice = 0, int DifficultyUpgrades = 0, bool RemoveBoosts = false)
{
    public static readonly CheckPenalty None = new(0, []);
}

/// <summary>Вклад предмета в проверки: развёрнутый <see cref="Entities.ItemCheckModifier"/> с именем предмета.</summary>
/// <param name="ItemName">Английское имя предмета — оно попадает в подсказку.</param>
/// <param name="ItemNameRu">Русское имя предмета.</param>
/// <param name="SkillName">Английское имя навыка; пусто — отбор идёт по характеристике.</param>
/// <param name="Characteristic">Характеристика проверки; <c>null</c> — отбор идёт по навыку.</param>
public sealed record ItemCheckModifierInput(
    string ItemName,
    string ItemNameRu,
    CheckModifierKind Kind,
    string SkillName,
    CharacteristicType? Characteristic,
    int Value,
    string Condition = "");

/// <summary>
/// Сводит все помехи, которые персонаж тащит на себе, к конкретной проверке навыка: штрафы
/// снаряжения (ROT-ARM-01) и перегруз (ROT-EQP-01). До этого перегруз был виден только в блоке
/// веса, а штраф брони не существовал вовсе — игрок бросал пул без них и не знал об этом.
/// </summary>
public static class CheckModifierAggregator
{
    /// <summary>Характеристики, к проверкам которых перегруз добавляет помехи.</summary>
    private static readonly CharacteristicType[] OverloadCharacteristics =
        [CharacteristicType.Brawn, CharacteristicType.Agility];

    /// <param name="skillNameEn">Английское имя навыка проверки.</param>
    /// <param name="characteristic">Характеристика, по которой строится пул навыка.</param>
    /// <param name="itemModifiers">
    /// Модификаторы уже отобранных предметов: вызывающий отсекает ненадетое и неактивную броню.
    /// </param>
    /// <param name="encumbrance">Состояние перегруза; <c>null</c> — не учитывать.</param>
    /// <param name="skillBoosts">
    /// Кости умения от установленных улучшений (ROT-EQP-ATT-01). Вызывающий отсекает те, что
    /// не действуют: у неактивной брони улучшение молчит так же, как её штраф.
    /// </param>
    public static CheckPenalty For(
        string skillNameEn,
        CharacteristicType characteristic,
        IReadOnlyList<ItemCheckModifierInput>? itemModifiers = null,
        EncumbranceState? encumbrance = null,
        IReadOnlyList<AttachmentSkillBoost>? skillBoosts = null,
        IReadOnlyList<CriticalInjuryRules.CheckModifier>? criticalInjuries = null)
    {
        var sources = new List<CheckModifierSource>();

        foreach (var m in itemModifiers ?? [])
        {
            if (!Applies(m, skillNameEn, characteristic)) continue;
            var signed = m.Kind == CheckModifierKind.RemoveSetback ? -Math.Abs(m.Value) : Math.Abs(m.Value);
            if (signed == 0) continue;
            sources.Add(new CheckModifierSource("Item", m.ItemName, m.ItemNameRu, signed, m.Condition));
        }

        // Перегруз — столько же помех, каково превышение, ко всем проверкам Мощи и Ловкости.
        if (encumbrance is { SetbackDice: > 0 } enc && OverloadCharacteristics.Contains(characteristic))
            sources.Add(new CheckModifierSource("Encumbrance", "Encumbered", "Перегруз", enc.SetbackDice));

        // Условные вклады показываются, но сами в пул не подставляются: приложение не знает,
        // холодная ли сейчас погода, и врать про это хуже, чем не считать.
        foreach (var b in skillBoosts ?? [])
        {
            if (b.Boost == 0 || !string.Equals(b.SkillName, skillNameEn, StringComparison.OrdinalIgnoreCase))
                continue;
            sources.Add(new CheckModifierSource("Attachment", b.SourceName, b.SourceNameRu, 0, "", b.Boost));
        }

        foreach (var injury in criticalInjuries ?? [])
        {
            if (!Applies(injury, skillNameEn, characteristic)) continue;
            if (injury.Setback == 0 && injury.Difficulty == 0 && injury.DifficultyUpgrades == 0
                && !injury.RemoveBoosts) continue;
            sources.Add(new CheckModifierSource(
                "CriticalInjury", injury.SourceName, injury.SourceNameRu, injury.Setback, "",
                0, injury.Difficulty, injury.DifficultyUpgrades, injury.RemoveBoosts));
        }

        var total = sources.Where(s => !s.IsConditional).Sum(s => s.Setback);
        var boost = sources.Where(s => !s.IsConditional).Sum(s => s.Boost);
        var difficulty = sources.Where(s => !s.IsConditional).Sum(s => s.Difficulty);
        var upgrades = sources.Where(s => !s.IsConditional).Sum(s => s.DifficultyUpgrades);
        var removeBoosts = sources.Any(s => !s.IsConditional && s.RemoveBoosts);
        return new CheckPenalty(
            Math.Max(0, total), sources, removeBoosts ? 0 : Math.Max(0, boost),
            Math.Max(0, difficulty), Math.Max(0, upgrades), removeBoosts);
    }

    private static bool Applies(ItemCheckModifierInput m, string skillNameEn, CharacteristicType characteristic)
    {
        if (!string.IsNullOrEmpty(m.SkillName))
            return string.Equals(m.SkillName, skillNameEn, StringComparison.OrdinalIgnoreCase);
        return m.Characteristic == characteristic;
    }

    private static bool Applies(
        CriticalInjuryRules.CheckModifier m, string skillNameEn, CharacteristicType characteristic)
    {
        if (!string.IsNullOrEmpty(m.SkillName))
            return string.Equals(m.SkillName, skillNameEn, StringComparison.OrdinalIgnoreCase);
        return m.Characteristic is null || m.Characteristic == characteristic;
    }
}
