namespace GenesysForge.Domain;

/// <summary>
/// Расчёт производных характеристик листа: HP/стрейн/защита/поглощение/переносимый вес
/// с учётом надетых предметов и пассивных талантов.
/// </summary>
public static class SheetCalculator
{
    public static DerivedStats ComputeDerived(
        CharacteristicsSet ch,
        int archetypeWoundBase,
        int archetypeStrainBase,
        IReadOnlyList<TalentInput> talents,
        IReadOnlyList<ItemInput> items)
    {
        var equipped = items.Where(i => i.State == ItemState.Equipped).ToList();

        var talentWounds = talents.Sum(t => t.WoundBonusPerRank * t.Ranks);
        var talentStrain = talents.Sum(t => t.StrainBonusPerRank * t.Ranks);
        var talentSoak = talents.Sum(t => t.SoakBonusPerRank * t.Ranks);
        var talentMeleeDef = talents.Sum(t => t.MeleeDefenseBonusPerRank * t.Ranks);
        var talentRangedDef = talents.Sum(t => t.RangedDefenseBonusPerRank * t.Ranks);

        var armorSoak = equipped.Sum(i => i.SoakBonus);
        // Защита из разных источников не складывается — берётся лучшая, таланты добавляются сверху.
        var meleeDef = equipped.Select(i => i.MeleeDefense).DefaultIfEmpty(0).Max() + talentMeleeDef;
        var rangedDef = equipped.Select(i => i.RangedDefense).DefaultIfEmpty(0).Max() + talentRangedDef;

        var encThreshold = GenesysRules.EncumbranceThreshold(
            ch.Brawn,
            equipped.Sum(i => i.EncumbranceThresholdBonus));

        var load = items.Sum(ItemLoad);

        return new DerivedStats(
            WoundThreshold: GenesysRules.WoundThreshold(archetypeWoundBase, ch.Brawn, talentWounds),
            StrainThreshold: GenesysRules.StrainThreshold(archetypeStrainBase, ch.Willpower, talentStrain),
            Soak: GenesysRules.Soak(ch.Brawn, armorSoak, talentSoak),
            MeleeDefense: meleeDef,
            RangedDefense: rangedDef,
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
