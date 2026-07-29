using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>
/// Предмет персонажа со всеми поправками: база каталога → качество изготовления (ROT-WPN-02) →
/// улучшения (ROT-EQP-ATT-01) → состояние повреждения (GEN-EQP-DMG-01). Одна точка расчёта на
/// всё приложение: лист, карточка, защита, помехи и профили атаки обязаны видеть одинаковые числа.
/// </summary>
/// <param name="Attachments">Улучшения, стоящие на предмете.</param>
/// <param name="HardPoints">Слоты предмета после поправок; <c>null</c> — книжного значения нет.</param>
/// <param name="UsedHardPoints">Сколько слотов занято.</param>
/// <param name="EncumbranceThresholdBonus">
/// Прибавка к порогу переносимого веса от контейнера. У сломанного контейнера отключена, но его
/// содержимое не теряется — оно лежит там же, просто мешок больше не помогает нести.
/// </param>
/// <param name="IsUsable">
/// Предметом можно пользоваться: Серьёзное и Уничтожено снимают все преимущества экземпляра.
/// </param>
public sealed record EffectiveItem(
    CharacterItem Item,
    ItemDef Def,
    int Encumbrance,
    int SoakBonus,
    int MeleeDefense,
    int RangedDefense,
    int? Price,
    int? Rarity,
    bool Reinforced,
    int? HardPoints,
    int UsedHardPoints,
    IReadOnlyList<EffectiveQuality> Qualities,
    IReadOnlyList<CharacterAttachment> Attachments,
    AttachmentAggregate AttachmentEffects,
    IReadOnlyList<ItemStatAdjustment> Adjustments,
    IReadOnlyList<CraftsmanshipCheckModifier> CheckModifiers,
    bool WornAndActive,
    bool IsActiveArmor,
    int EncumbranceThresholdBonus = 0,
    ItemDamageState DamageState = ItemDamageState.Undamaged,
    bool IsUsable = true,
    /// <summary>Паспорт магического инструмента; <c>null</c> — запись инструментом не является.</summary>
    ImplementSpec? Implement = null)
{
    /// <summary>Свободные слоты улучшений; у записи без книжного значения — по Core-запасному расчёту.</summary>
    public int RemainingHardPoints => Math.Max(0, (HardPoints ?? 0) - UsedHardPoints);

    /// <summary>Улучшений стоит больше, чем осталось слотов (после уменьшения HP).</summary>
    public bool OverCapacity => HardPoints is { } hp && UsedHardPoints > hp;
}

/// <summary>Сборка <see cref="EffectiveItem"/> из строк персонажа.</summary>
public static class EffectiveItems
{
    /// <summary>Все позиции инвентаря персонажа с посчитанными поправками.</summary>
    public static List<EffectiveItem> For(Character c) =>
        [.. c.Items.Where(i => i.ItemDef is not null).Select(i => For(c, i))];

    /// <summary>Одна позиция инвентаря с посчитанными поправками.</summary>
    public static EffectiveItem For(Character c, CharacterItem item)
    {
        var def = item.ItemDef!;
        var stats = CraftsmanshipRules.For(def, item.Craftsmanship);

        // Материал магического инструмента (ROT-MAG-MAT-01): он меняет цену и редкость экземпляра
        // тем же порядком, что и качество изготовления, — до улучшений и состояния.
        var implement = ImplementRules.For(def.Code);
        int? materialPrice = def.Price is null
            ? null
            : implement is null
                ? stats.Price
                : ImplementRules.Price(stats.Price, item.ImplementMaterial);
        int? materialRarity = def.Rarity is null
            ? null
            : implement is null
                ? stats.Rarity
                : ImplementRules.Rarity(stats.Rarity, item.ImplementMaterial);

        // Надета и действует: неактивная броня даёт только вес — и её улучшения молчат так же,
        // как её собственные штрафы (ROT-CMB-02).
        var wornAndActive = item.State == ItemState.Equipped
            && (def.Kind != ItemKind.Armor || item.Id == c.ActiveArmorCharacterItemId);

        var installed = c.Attachments
            .Where(a => a.HostCharacterItemId == item.Id && a.AttachmentDef is not null)
            .OrderBy(a => a.AttachmentDef!.NameRu, StringComparer.Ordinal)
            .ToList();
        // Сломанное улучшение эффекта не даёт, но слот занимать не перестаёт: слот освобождает
        // снятие, а не повреждение (GEN-EQP-DMG-01). Поэтому оно выпадает из inputs, но остаётся
        // в installed, по которому считаются занятые слоты.
        var allInputs = installed
            .Select(a => new AttachmentInput(a.AttachmentDef!, wornAndActive))
            .ToList();

        // Серьёзно повреждённый предмет не даёт вообще ничего: ни своих чисел, ни эффектов
        // улучшений, стоящих на нём. Вес и содержимое при этом остаются.
        var usable = DamageStateRules.IsUsable(item.DamageState);
        var inputs = usable
            ? [.. installed
                .Where(a => DamageStateRules.IsUsable(a.DamageState))
                .Select(a => new AttachmentInput(a.AttachmentDef!, wornAndActive))]
            : new List<AttachmentInput>();
        var aggregate = AttachmentRules.Aggregate(inputs);

        var baseQualities = def.Qualities
            .Where(q => q.QualityDef is not null)
            .Select(q => new EffectiveQuality(q.QualityDef!.Code, q.Rating ?? 0));
        var qualities = AttachmentRules.ApplyQualities(baseQualities, inputs);
        if (stats.Reinforced && qualities.All(q => q.Code != CraftsmanshipRules.ReinforcedQualityCode))
            qualities = [.. qualities, new EffectiveQuality(CraftsmanshipRules.ReinforcedQualityCode, 0)];

        // Вес улучшений остаётся и у сломанного предмета: наручи никуда не делись, они просто
        // больше не работают.
        var encumbrance = Math.Max(0, stats.Encumbrance + AttachmentRules.Aggregate(allInputs).Encumbrance);
        var soak = stats.SoakBonus + aggregate.SoakBonus;
        var melee = stats.MeleeDefense + aggregate.MeleeDefense;
        var ranged = stats.RangedDefense + aggregate.RangedDefense;
        var encThreshold = def.EncumbranceThresholdBonus;

        // Слоты: книжное значение с поправкой работы, а у записи без него — Core-запасной расчёт
        // от базового веса. «Значения нет» и «ноль слотов» — разные вещи только для книги.
        var hardPoints = stats.HardPoints ?? AttachmentRules.FallbackHardPoints(def.Encumbrance);
        var used = AttachmentRules.UsedHardPoints(installed.Select(a => a.AttachmentDef!));

        var adjustments = new List<ItemStatAdjustment>(stats.Adjustments);
        if (materialPrice is { } effectivePrice && effectivePrice != stats.Price)
            adjustments.Add(new ItemStatAdjustment(
                "price", stats.Price, effectivePrice, ItemStatStage.Material,
                item.ImplementMaterial.ToString()));
        if (materialRarity is { } effectiveRarity && effectiveRarity != stats.Rarity)
            adjustments.Add(new ItemStatAdjustment(
                "rarity", stats.Rarity, effectiveRarity, ItemStatStage.Material,
                item.ImplementMaterial.ToString()));
        void Track(string field, int before, int after)
        {
            if (before != after) adjustments.Add(new ItemStatAdjustment(
                field, before, after, ItemStatStage.Attachments, "Attachments"));
        }
        Track("encumbrance", stats.Encumbrance, encumbrance);
        Track("soak", stats.SoakBonus, soak);
        Track("meleeDefense", stats.MeleeDefense, melee);
        Track("rangedDefense", stats.RangedDefense, ranged);

        // Состояние повреждения — последний этап расчёта (GEN-EQP-DMG-01): серьёзно повреждённый
        // предмет теряет поглощение, защиту и прибавку к порогу веса. Каждая потеря попадает в
        // разбор, иначе на карточке молча стояли бы нули без объяснения.
        if (!usable)
        {
            var source = item.DamageState.ToString();
            void TrackDamage(string field, int before)
            {
                if (before != 0) adjustments.Add(new ItemStatAdjustment(
                    field, before, 0, ItemStatStage.DamageState, source));
            }
            TrackDamage("soak", soak);
            TrackDamage("meleeDefense", melee);
            TrackDamage("rangedDefense", ranged);
            TrackDamage("encumbranceThreshold", encThreshold);
            soak = melee = ranged = encThreshold = 0;
        }

        return new EffectiveItem(
            item, def, encumbrance, soak, melee, ranged, materialPrice, materialRarity,
            stats.Reinforced || qualities.Any(q => q.Code == CraftsmanshipRules.ReinforcedQualityCode),
            hardPoints, used, qualities, installed, aggregate, adjustments, stats.CheckModifiers,
            wornAndActive, item.Id == c.ActiveArmorCharacterItemId,
            encThreshold, item.DamageState, usable, implement);
    }

    /// <summary>
    /// Кости умения от улучшений всех действующих предметов. Отбор здесь, а не в доменном
    /// агрегаторе: правило «только надетое и активное» — про инвентарь, а не про конкретную проверку.
    /// </summary>
    public static List<AttachmentSkillBoost> SkillBoosts(IEnumerable<EffectiveItem> items) =>
        [.. items.SelectMany(i => i.AttachmentEffects.SkillBoosts)];
}
