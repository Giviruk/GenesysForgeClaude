using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>
/// Предмет персонажа со всеми поправками: база каталога → качество изготовления (ROT-WPN-02) →
/// улучшения (ROT-EQP-ATT-01). Одна точка расчёта на всё приложение: лист, карточка, защита,
/// помехи и профили атаки обязаны видеть одинаковые числа.
/// </summary>
/// <param name="Attachments">Улучшения, стоящие на предмете.</param>
/// <param name="HardPoints">Слоты предмета после поправок; <c>null</c> — книжного значения нет.</param>
/// <param name="UsedHardPoints">Сколько слотов занято.</param>
public sealed record EffectiveItem(
    CharacterItem Item,
    ItemDef Def,
    int Encumbrance,
    int SoakBonus,
    int MeleeDefense,
    int RangedDefense,
    int Price,
    int Rarity,
    bool Reinforced,
    int? HardPoints,
    int UsedHardPoints,
    IReadOnlyList<EffectiveQuality> Qualities,
    IReadOnlyList<CharacterAttachment> Attachments,
    AttachmentAggregate AttachmentEffects,
    IReadOnlyList<ItemStatAdjustment> Adjustments,
    IReadOnlyList<CraftsmanshipCheckModifier> CheckModifiers,
    bool WornAndActive,
    bool IsActiveArmor)
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

        // Надета и действует: неактивная броня даёт только вес — и её улучшения молчат так же,
        // как её собственные штрафы (ROT-CMB-02).
        var wornAndActive = item.State == ItemState.Equipped
            && (def.Kind != ItemKind.Armor || item.Id == c.ActiveArmorCharacterItemId);

        var installed = c.Attachments
            .Where(a => a.HostCharacterItemId == item.Id && a.AttachmentDef is not null)
            .OrderBy(a => a.AttachmentDef!.NameRu, StringComparer.Ordinal)
            .ToList();
        var inputs = installed
            .Select(a => new AttachmentInput(a.AttachmentDef!, wornAndActive))
            .ToList();
        var aggregate = AttachmentRules.Aggregate(inputs);

        var baseQualities = def.Qualities
            .Where(q => q.QualityDef is not null)
            .Select(q => new EffectiveQuality(q.QualityDef!.Code, q.Rating ?? 0));
        var qualities = AttachmentRules.ApplyQualities(baseQualities, inputs);
        if (stats.Reinforced && qualities.All(q => q.Code != CraftsmanshipRules.ReinforcedQualityCode))
            qualities = [.. qualities, new EffectiveQuality(CraftsmanshipRules.ReinforcedQualityCode, 0)];

        var encumbrance = Math.Max(0, stats.Encumbrance + aggregate.Encumbrance);
        var soak = stats.SoakBonus + aggregate.SoakBonus;
        var melee = stats.MeleeDefense + aggregate.MeleeDefense;
        var ranged = stats.RangedDefense + aggregate.RangedDefense;

        // Слоты: книжное значение с поправкой работы, а у записи без него — Core-запасной расчёт
        // от базового веса. «Значения нет» и «ноль слотов» — разные вещи только для книги.
        var hardPoints = stats.HardPoints ?? AttachmentRules.FallbackHardPoints(def.Encumbrance);
        var used = AttachmentRules.UsedHardPoints(installed.Select(a => a.AttachmentDef!));

        var adjustments = new List<ItemStatAdjustment>(stats.Adjustments);
        void Track(string field, int before, int after)
        {
            if (before != after) adjustments.Add(new ItemStatAdjustment(
                field, before, after, ItemStatStage.Attachments, "Attachments"));
        }
        Track("encumbrance", stats.Encumbrance, encumbrance);
        Track("soak", stats.SoakBonus, soak);
        Track("meleeDefense", stats.MeleeDefense, melee);
        Track("rangedDefense", stats.RangedDefense, ranged);

        return new EffectiveItem(
            item, def, encumbrance, soak, melee, ranged, stats.Price, stats.Rarity,
            stats.Reinforced || qualities.Any(q => q.Code == CraftsmanshipRules.ReinforcedQualityCode),
            hardPoints, used, qualities, installed, aggregate, adjustments, stats.CheckModifiers,
            wornAndActive, item.Id == c.ActiveArmorCharacterItemId);
    }

    /// <summary>
    /// Кости умения от улучшений всех действующих предметов. Отбор здесь, а не в доменном
    /// агрегаторе: правило «только надетое и активное» — про инвентарь, а не про конкретную проверку.
    /// </summary>
    public static List<AttachmentSkillBoost> SkillBoosts(IEnumerable<EffectiveItem> items) =>
        [.. items.SelectMany(i => i.AttachmentEffects.SkillBoosts)];
}
