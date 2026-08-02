using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain;

/// <summary>
/// Расчёт производных характеристик листа: HP/стрейн/защита/поглощение/переносимый вес
/// с учётом надетых предметов и пассивных талантов.
/// </summary>
public static class SheetCalculator
{
    /// <param name="woundThresholdSnapshot">
    /// Зафиксированный при завершении создания порог ран (ROT-CRE-02). Если задан, база и Brawn
    /// больше не участвуют в расчёте: явные бонусы талантов прибавляются поверх snapshot ровно один раз.
    /// </param>
    /// <param name="strainThresholdSnapshot">То же для порога стрейна.</param>
    /// <param name="baseDefense">
    /// Базовая защита, задаваемая видом (Nimble, ROT-SPECIES-01). Это установка значения, а не
    /// прибавка: она конкурирует с бронёй за максимум и не складывается с ней.
    /// </param>
    public static DerivedStats ComputeDerived(
        CharacteristicsSet ch,
        int archetypeWoundBase,
        int archetypeStrainBase,
        IReadOnlyList<TalentInput> talents,
        IReadOnlyList<ItemInput> items,
        int? woundThresholdSnapshot = null,
        int? strainThresholdSnapshot = null,
        int? baseDefense = null,
        IReadOnlyList<DefenseContribution>? extraDefense = null)
    {
        // Метнутое и не подобранное оружие лежит у цели: ни качеств, ни защиты, ни веса (ROT-WPN-01).
        var equipped = items.Where(i => i.State == ItemState.Equipped && !i.IsThrown).ToList();

        var thresholds = ComputeThresholds(
            ch, archetypeWoundBase, archetypeStrainBase, talents,
            woundThresholdSnapshot, strainThresholdSnapshot);
        var talentSoak = talents.Sum(t => t.SoakBonusPerRank * t.Ranks);

        // Персонаж может носить несколько броней, но защиту и поглощение даёт ровно одна выбранная
        // (ROT-CMB-02). Не-броня (щит, талисман) считается отдельно и активной броней не является.
        var protective = equipped
            .Where(i => i.Kind != ItemKind.Armor || i.IsActiveArmor)
            .ToList();

        var armorSoak = protective.Sum(i => i.SoakBonus);

        // Защита сводится по ROT-CMB-03: источники «получает Defense N» (броня, укрытие, видовая
        // Nimble) не складываются между собой — берётся лучший; надбавки «+N» от талантов
        // складываются с ним и друг с другом; итог ограничен четырьмя.
        var defenseContributions = new List<DefenseContribution>();
        foreach (var item in protective)
            defenseContributions.AddRange(ItemDefense(item));
        if (baseDefense is > 0)
            defenseContributions.Add(new DefenseContribution(
                "Species", "Nimble", DefenseScope.General, DefenseMode.Provides, baseDefense.Value));
        foreach (var talent in talents)
        {
            if (talent.MeleeDefenseBonusPerRank != 0 && talent.Ranks > 0)
                defenseContributions.Add(new DefenseContribution(
                    "Talent", talent.Name, DefenseScope.Melee, DefenseMode.Increases,
                    talent.MeleeDefenseBonusPerRank * talent.Ranks));
            if (talent.RangedDefenseBonusPerRank != 0 && talent.Ranks > 0)
                defenseContributions.Add(new DefenseContribution(
                    "Talent", talent.Name, DefenseScope.Ranged, DefenseMode.Increases,
                    talent.RangedDefenseBonusPerRank * talent.Ranks));
        }
        foreach (var extra in extraDefense ?? [])
            defenseContributions.Add(extra);

        var meleeBreakdown = DefenseAggregator.Melee(defenseContributions);
        var rangedBreakdown = DefenseAggregator.Ranged(defenseContributions);
        var meleeDef = meleeBreakdown.Effective;
        var rangedDef = rangedBreakdown.Effective;

        // Предметы с нулевым Enc не пропадают: они считаются вместе по всем позициям (ROT-EQP-01).
        var zeroEncUnits = items
            .Where(i => i.Encumbrance == 0 && i.Kind != ItemKind.Armor)
            .Sum(i => Math.Max(1, i.Quantity));
        var encumbrance = EncumbranceRules.Compute(
            ch.Brawn,
            items.Sum(ItemLoad),
            protective.Sum(i => i.EncumbranceThresholdBonus),
            EncumbranceRules.ZeroEncumbranceLoad(zeroEncUnits));
        var encThreshold = encumbrance.Threshold;
        var load = encumbrance.Load;

        return new DerivedStats(
            WoundThreshold: thresholds.Wound,
            StrainThreshold: thresholds.Strain,
            Soak: GenesysRules.Soak(ch.Brawn, armorSoak, talentSoak),
            MeleeDefense: meleeDef,
            RangedDefense: rangedDef,
            EncumbranceThreshold: encThreshold,
            EncumbranceLoad: load,
            Encumbered: encumbrance.Encumbered,
            MeleeDefenseBreakdown: meleeBreakdown,
            RangedDefenseBreakdown: rangedBreakdown,
            Encumbrance: encumbrance);
    }

    /// <summary>
    /// Считает только пороги ран и стрейна. Карточкам списка персонажей не нужны предметы,
    /// защита и вес, поэтому они используют этот узкий расчёт и не загружают инвентарь.
    /// Формула остаётся общей с полным листом через вызов из <see cref="ComputeDerived"/>.
    /// </summary>
    public static (int Wound, int Strain) ComputeThresholds(
        CharacteristicsSet ch,
        int archetypeWoundBase,
        int archetypeStrainBase,
        IReadOnlyList<TalentInput> talents,
        int? woundThresholdSnapshot = null,
        int? strainThresholdSnapshot = null)
    {
        var talentWounds = talents.Sum(t => t.WoundBonusPerRank * t.Ranks);
        var talentStrain = talents.Sum(t => t.StrainBonusPerRank * t.Ranks);
        return (
            woundThresholdSnapshot is { } wt
                ? wt + talentWounds
                : GenesysRules.WoundThreshold(archetypeWoundBase, ch.Brawn, talentWounds),
            strainThresholdSnapshot is { } st
                ? st + talentStrain
                : GenesysRules.StrainThreshold(archetypeStrainBase, ch.Willpower, talentStrain));
    }

    /// <summary>Вес позиции инвентаря: надетая броня — encumbrance −3 (мин. 0), остальное полностью.</summary>
    public static int ItemLoad(ItemInput item)
    {
        // Метнутое оружие персонаж на себе не несёт, пока не подобрал.
        if (item.IsThrown) return 0;
        var perUnit = item is { State: ItemState.Equipped, Kind: ItemKind.Armor }
            ? GenesysRules.WornArmorEncumbrance(item.Encumbrance)
            : item.Encumbrance;
        return perUnit * Math.Max(1, item.Quantity);
    }

    /// <summary>Коды качеств, дающих надбавку к защите (ROT-WPN-01).</summary>
    private const string DefensiveCode = "defensive";
    private const string DeflectionCode = "deflection";

    /// <summary>
    /// Вклад предмета в защиту. Броня «получает Defense N» и потому конкурирует за максимум
    /// (ROT-CMB-03). Щит — оружие, а не броня: его Defensive и Deflection — надбавки «+N», которые
    /// складываются с бронёй и действуют, пока щит в руках, даже при атаке другим оружием.
    /// Числовые колонки предмета используются только там, где структурных качеств нет: у кастомного
    /// снаряжения. Иначе один и тот же щит посчитался бы дважды.
    /// </summary>
    private static IEnumerable<DefenseContribution> ItemDefense(ItemInput item)
    {
        if (item.Kind == ItemKind.Armor)
        {
            if (item.MeleeDefense > 0)
                yield return new DefenseContribution(
                    "Item", item.Name, DefenseScope.Melee, DefenseMode.Provides, item.MeleeDefense);
            if (item.RangedDefense > 0)
                yield return new DefenseContribution(
                    "Item", item.Name, DefenseScope.Ranged, DefenseMode.Provides, item.RangedDefense);
            yield break;
        }

        var defensive = QualityRating(item, DefensiveCode);
        var deflection = QualityRating(item, DeflectionCode);
        if (defensive > 0 || deflection > 0)
        {
            if (defensive > 0)
                yield return new DefenseContribution(
                    "Item", item.Name, DefenseScope.Melee, DefenseMode.Increases, defensive);
            if (deflection > 0)
                yield return new DefenseContribution(
                    "Item", item.Name, DefenseScope.Ranged, DefenseMode.Increases, deflection);
            yield break;
        }

        if (item.MeleeDefense > 0)
            yield return new DefenseContribution(
                "Item", item.Name, DefenseScope.Melee, DefenseMode.Increases, item.MeleeDefense);
        if (item.RangedDefense > 0)
            yield return new DefenseContribution(
                "Item", item.Name, DefenseScope.Ranged, DefenseMode.Increases, item.RangedDefense);
    }

    private static int QualityRating(ItemInput item, string code) =>
        (item.Qualities ?? [])
            .Where(q => string.Equals(q.Code, code, StringComparison.OrdinalIgnoreCase))
            .Sum(q => Math.Max(0, q.Rating));

    public static List<SkillComputed> ComputeSkills(CharacteristicsSet ch, IEnumerable<SkillInput> skills) =>
        skills
            .Select(s => new SkillComputed(
                s.Name, s.Characteristic, s.Ranks, s.IsCareer,
                GenesysRules.BuildDicePool(ch.Get(s.Characteristic), s.Ranks)))
            .ToList();
}
