using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>
/// Единственная точка расчёта производных характеристик персонажа (ROT-CRE-02).
/// Лист, список персонажей, печать, duplicate, кампания и Game Table обязаны использовать её,
/// иначе зафиксированные при создании пороги разойдутся между поверхностями.
/// </summary>
public static class CharacterDerived
{
    public static DerivedStats Compute(Character c) =>
        SheetCalculator.ComputeDerived(
            c.Characteristics,
            c.Archetype!.WoundBase,
            c.Archetype.StrainBase,
            TalentInputs(c),
            ItemInputs(c),
            c.CreationWoundThreshold,
            c.CreationStrainThreshold,
            BaseDefense(c));

    public static List<TalentInput> TalentInputs(Character c) => c.Talents
        .Where(t => t.TalentDef is not null)
        .Select(t => new TalentInput(
            t.TalentDef!.Name, t.TalentDef.Tier, t.Ranks,
            t.TalentDef.WoundBonus, t.TalentDef.StrainBonus, t.TalentDef.SoakBonus,
            t.TalentDef.MeleeDefenseBonus, t.TalentDef.RangedDefenseBonus))
        .ToList();

    public static List<ItemInput> ItemInputs(Character c) => c.Items
        .Where(i => i.ItemDef is not null)
        .Select(i => new ItemInput(
            i.ItemDef!.Name, i.ItemDef.Kind, i.State, i.ItemDef.Encumbrance, i.Quantity,
            i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense, i.ItemDef.RangedDefense,
            i.ItemDef.EncumbranceThresholdBonus,
            i.Id == c.ActiveArmorCharacterItemId,
            [.. i.ItemDef.Qualities
                .Where(q => q.QualityDef is not null)
                .Select(q => new ItemQualityInput(q.QualityDef!.Code, q.Rating ?? 0))],
            i.IsThrown))
        .ToList();

    /// <summary>
    /// Модификаторы проверок от снаряжения (ROT-ARM-01). Отбор здесь, а не в доменном агрегаторе:
    /// правило «действует только надетое» и правило активной брони (ROT-CMB-02) — про инвентарь,
    /// а не про конкретную проверку.
    /// </summary>
    public static List<ItemCheckModifierInput> CheckModifierInputs(Character c) => c.Items
        .Where(i => i.ItemDef is not null)
        .SelectMany(i => i.ItemDef!.CheckModifiers.Select(m => (Item: i, Def: i.ItemDef!, Mod: m)))
        .Where(x => !x.Mod.RequiresWorn || IsWornAndEffective(c, x.Item, x.Def))
        .Select(x => new ItemCheckModifierInput(
            x.Def.Name, x.Def.NameRu, x.Mod.Kind, x.Mod.SkillName, x.Mod.Characteristic,
            x.Mod.Value, x.Mod.Condition))
        .ToList();

    /// <summary>
    /// Надет и действует: неактивная надетая броня даёт только вес, поэтому её штрафы тоже не
    /// применяются — иначе две надетые кольчуги штрафовали бы Скрытность дважды.
    /// </summary>
    private static bool IsWornAndEffective(Character c, CharacterItem item, ItemDef def) =>
        item.State == ItemState.Equipped
        && (def.Kind != ItemKind.Armor || item.Id == c.ActiveArmorCharacterItemId);

    /// <summary>
    /// Базовая защита от видовых способностей, действующих для этого персонажа (Nimble).
    /// У вида с обязательным выбором учитывается только сделанный выбор.
    /// </summary>
    public static int? BaseDefense(Character c)
    {
        if (c.Archetype is null) return null;
        var byCode = c.Archetype.Abilities.ToDictionary(a => a.Code, StringComparer.Ordinal);
        return SpeciesAbilityRules.BaseDefense(
            SpeciesAbilityRules.EffectiveAbilities(c.Archetype, c.SpeciesAbilityChoiceCode, byCode));
    }

    /// <summary>Итоговый silhouette персонажа: база вида, перекрытая способностью <c>Small</c>.</summary>
    public static int Silhouette(Character c) =>
        c.Archetype is null ? 1 : SpeciesAbilityRules.Silhouette(c.Archetype);

    /// <summary>
    /// Пороги на момент завершения создания: база вида плюс текущая характеристика, без бонусов
    /// талантов — они прибавляются поверх snapshot при каждом расчёте.
    /// </summary>
    public static (int Wound, int Strain) CreationSnapshot(Character c) => (
        GenesysRules.WoundThreshold(c.Archetype!.WoundBase, c.Brawn),
        GenesysRules.StrainThreshold(c.Archetype.StrainBase, c.Willpower));
}
