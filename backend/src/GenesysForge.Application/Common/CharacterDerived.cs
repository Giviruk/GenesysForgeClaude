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
            i.ItemDef.EncumbranceThresholdBonus))
        .ToList();

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
