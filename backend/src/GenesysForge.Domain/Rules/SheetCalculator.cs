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
        var equipped = items.Where(i => i.State == ItemState.Equipped).ToList();

        var talentWounds = talents.Sum(t => t.WoundBonusPerRank * t.Ranks);
        var talentStrain = talents.Sum(t => t.StrainBonusPerRank * t.Ranks);
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
        {
            if (item.MeleeDefense > 0)
                defenseContributions.Add(new DefenseContribution(
                    "Item", item.Name, DefenseScope.Melee, DefenseMode.Provides, item.MeleeDefense));
            if (item.RangedDefense > 0)
                defenseContributions.Add(new DefenseContribution(
                    "Item", item.Name, DefenseScope.Ranged, DefenseMode.Provides, item.RangedDefense));
        }
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

        var encThreshold = GenesysRules.EncumbranceThreshold(
            ch.Brawn,
            protective.Sum(i => i.EncumbranceThresholdBonus));

        var load = items.Sum(ItemLoad);

        return new DerivedStats(
            WoundThreshold: woundThresholdSnapshot is { } wt
                ? wt + talentWounds
                : GenesysRules.WoundThreshold(archetypeWoundBase, ch.Brawn, talentWounds),
            StrainThreshold: strainThresholdSnapshot is { } st
                ? st + talentStrain
                : GenesysRules.StrainThreshold(archetypeStrainBase, ch.Willpower, talentStrain),
            Soak: GenesysRules.Soak(ch.Brawn, armorSoak, talentSoak),
            MeleeDefense: meleeDef,
            RangedDefense: rangedDef,
            MeleeDefenseBreakdown: meleeBreakdown,
            RangedDefenseBreakdown: rangedBreakdown,
            EncumbranceThreshold: encThreshold,
            EncumbranceLoad: load,
            Encumbered: load > encThreshold);
    }

    /// <summary>Вес позиции инвентаря: надетая броня — encumbrance −3 (мин. 0), остальное полностью.</summary>
    public static int ItemLoad(ItemInput item)
    {
        var perUnit = item is { State: ItemState.Equipped, Kind: ItemKind.Armor }
            ? GenesysRules.WornArmorEncumbrance(item.Encumbrance)
            : item.Encumbrance;
        return perUnit * Math.Max(1, item.Quantity);
    }

    public static List<SkillComputed> ComputeSkills(CharacteristicsSet ch, IEnumerable<SkillInput> skills) =>
        skills
            .Select(s => new SkillComputed(
                s.Name, s.Characteristic, s.Ranks, s.IsCareer,
                GenesysRules.BuildDicePool(ch.Get(s.Characteristic), s.Ranks)))
            .ToList();
}
