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

    /// <summary>
    /// Вес, поглощение и защита экземпляров. Числа берутся уже с учётом качества изготовления
    /// (ROT-WPN-02): железная броня тяжелее каталожной, эльфийская легче, и лист обязан считать
    /// по экземпляру, а не по записи справочника.
    /// </summary>
    public static List<ItemInput> ItemInputs(Character c) =>
        [.. EffectiveItems.For(c).Select(ItemInput)];

    /// <summary>
    /// Одна позиция для расчёта листа: числа и качества уже со всеми поправками. У серьёзно
    /// повреждённого предмета качеств нет вовсе (GEN-EQP-DMG-01) — сломанный щит не защищает,
    /// хотя и продолжает висеть на руке и весить.
    /// </summary>
    private static ItemInput ItemInput(EffectiveItem e) => new(
        e.Def.Name, e.Def.Kind, e.Item.State, e.Encumbrance, e.Item.Quantity,
        e.SoakBonus, e.MeleeDefense, e.RangedDefense,
        e.EncumbranceThresholdBonus,
        e.IsActiveArmor,
        e.IsUsable ? [.. e.Qualities.Select(q => new ItemQualityInput(q.Code, q.Rating))] : [],
        e.Item.IsThrown);

    /// <summary>
    /// Модификаторы проверок от снаряжения (ROT-ARM-01). Отбор здесь, а не в доменном агрегаторе:
    /// правило «действует только надетое» и правило активной брони (ROT-CMB-02) — про инвентарь,
    /// а не про конкретную проверку.
    /// </summary>
    public static List<ItemCheckModifierInput> CheckModifierInputs(Character c) => c.Items
        .Where(i => i.ItemDef is not null && i.CarriedByMountId is null)
        .SelectMany(i => CatalogModifiers(i).Concat(CraftsmanshipModifiers(i)))
        .Where(x => !x.RequiresWorn || IsWornAndEffective(c, x.Item, x.Item.ItemDef!))
        .Where(x => IsStillInEffect(x.Item, x.Input))
        .Select(x => x.Input)
        .ToList();

    /// <summary>
    /// Сломанный предмет перестаёт помогать, но не перестаёт мешать (GEN-EQP-DMG-01): снятие
    /// помехи — преимущество экземпляра и с Серьёзным повреждением пропадает, а вот вес разбитых
    /// лат по-прежнему мешает красться. Помеха самого состояния сюда не попадает: она относится
    /// к проверкам этим предметом, а не ко всем проверкам навыка, и живёт в пуле предмета.
    /// </summary>
    private static bool IsStillInEffect(CharacterItem item, ItemCheckModifierInput input) =>
        DamageStateRules.IsUsable(item.DamageState) || input.Kind != CheckModifierKind.RemoveSetback;

    private static IEnumerable<(CharacterItem Item, bool RequiresWorn, ItemCheckModifierInput Input)>
        CatalogModifiers(CharacterItem i) => i.ItemDef!.CheckModifiers.Select(m => (i, m.RequiresWorn,
            new ItemCheckModifierInput(
                i.ItemDef!.Name, i.ItemDef.NameRu, m.Kind, m.SkillName, m.Characteristic,
                m.Value, m.Condition)));

    /// <summary>
    /// Помехи от качества изготовления (ROT-WPN-02): железная броня мешает четырём навыкам,
    /// эльфийская снимает одну помеху со Скрытности. Они действуют только на надетой активной
    /// броне — по тому же правилу, что и книжные штрафы записи каталога.
    /// </summary>
    private static IEnumerable<(CharacterItem Item, bool RequiresWorn, ItemCheckModifierInput Input)>
        CraftsmanshipModifiers(CharacterItem i) =>
        CraftsmanshipRules.CheckModifiers(i.ItemDef!.Kind, i.Craftsmanship)
            .Select(m => (i, true, new ItemCheckModifierInput(
                i.ItemDef!.Name, i.ItemDef.NameRu, m.Kind, m.SkillName, null, m.Value)));

    /// <summary>
    /// Надет и действует: неактивная надетая броня даёт только вес, поэтому её штрафы тоже не
    /// применяются — иначе две надетые кольчуги штрафовали бы Скрытность дважды.
    /// </summary>
    private static bool IsWornAndEffective(Character c, CharacterItem item, ItemDef def) =>
        item.State == ItemState.Equipped
        && (def.Kind != ItemKind.Armor || item.Id == c.ActiveArmorCharacterItemId);

    /// <summary>
    /// Что персонаж уже держит и носит: вход для проверки свободных рук и лимита брони
    /// (ROT-EQP-01). Метнутое оружие в руках не считается — оно лежит у цели.
    /// </summary>
    /// <param name="exceptItemId">Позиция, которую как раз надевают: её исключают из занятого.</param>
    public static List<EquippedItemInput> EquippedInputs(Character c, Guid? exceptItemId = null) =>
        [.. c.Items
            .Where(i => i.ItemDef is not null && i.State == ItemState.Equipped && !i.IsThrown
                && i.CarriedByMountId is null && i.Id != exceptItemId)
            .Select(i => new EquippedItemInput(
                i.Id, i.ItemDef!.Kind, i.ItemDef.FormTraits, i.ItemDef.Name,
                ImplementRules.IsImplement(i.ItemDef.Code)
                || RuneboundShardRules.IsShard(i.ItemDef.Code)))];

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
