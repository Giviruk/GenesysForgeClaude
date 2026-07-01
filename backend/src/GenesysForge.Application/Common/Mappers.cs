using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

public static class Mappers
{
    public static ArchetypeDto ToDto(this ArchetypeDef a) => new(a.Id, a.Name, a.NameRu, a.Brawn, a.Agility,
        a.Intellect, a.Cunning, a.Willpower, a.Presence, a.WoundBase, a.StrainBase, a.StartingXp,
        a.Description, a.SafeDescription, a.Source, a.OwnerUserId != null,
        a.Abilities.Select(x => new ArchetypeAbilityDto(x.Code, x.NameRu, x.NameEn, x.SafeDescription, x.AutomationKind)).ToList(),
        a.StartingSkills.Select(x => new ArchetypeStartingSkillDto(x.SkillName, x.NameRu, x.FreeRanks, x.IsChoice, x.ChoiceGroup, x.ChoiceCount)).ToList());

    public static CareerDto ToDto(this CareerDef c) =>
        new(c.Id, c.Name, c.NameRu, c.Description, c.SafeDescription, c.Source, c.OwnerUserId != null, c.CareerSkillNames,
            c.StartingMoneyFixed, c.StartingMoneyDice,
            c.StartingGear.Select(g => new CareerStartingGearDto(g.ItemCode, g.ItemNameFallback, g.Quantity,
                g.IsChoice, g.ChoiceGroup, g.ChoiceOption)).ToList(),
            c.Rules.Select(r => new CareerRuleDto(r.Code, r.Kind, r.Description)).ToList());

    public static SkillDefDto ToDto(this SkillDef s) =>
        new(s.Id, s.Name, s.NameRu, s.Characteristic, s.Kind, s.SafeDescription, s.Source, s.OwnerUserId != null);

    public static TalentDefDto ToDto(this TalentDef t) => new(t.Id, t.Name, t.NameRu, t.Tier, t.IsRanked, t.Category, t.Setting,
        t.Activation, t.Description, t.SafeDescription, t.Source,
        t.WoundBonus, t.StrainBonus, t.SoakBonus, t.MeleeDefenseBonus, t.RangedDefenseBonus, t.OwnerUserId != null,
        t.GrantsCharacteristic);

    public static ItemDefDto ToDto(this ItemDef i) => new(i.Id, i.Name, i.NameRu, i.Kind, i.Encumbrance, i.SoakBonus,
        i.MeleeDefense, i.RangedDefense, i.EncumbranceThresholdBonus,
        i.Description, i.SafeDescription, i.Source, i.Price, i.Rarity,
        i.SkillName, i.Damage, i.Crit, i.RangeBand, i.Properties, i.OwnerUserId != null,
        i.Qualities
            .Where(q => q.QualityDef != null)
            .Select(q => new ItemQualityRefDto(
                q.QualityDef!.Code, q.QualityDef.NameRu, q.QualityDef.NameEn, q.Rating,
                q.QualityDef.HasRating, q.QualityDef.IsActive, q.QualityDef.ActivationCost))
            .ToList());

    public static QualityDto ToDto(this QualityDef q) => new(q.Id, q.Code, q.NameEn, q.NameRu, q.Kind,
        q.IsActive, q.HasRating, q.ActivationCost, q.Category, q.Description, q.SafeDescription, q.Source);

    public static RuleTableEntryDto ToDto(this RuleTableEntry r) => new(r.Id, r.Kind, r.Code, r.NameRu,
        r.NameEn, r.GroupRu, r.SortOrder, r.RollRange, r.SymbolCost, r.Body, r.Notes, r.Source, r.SourcePage);

    public static HeroicAbilityDto ToDto(this HeroicAbilityDef h) =>
        new(h.Id, h.Code, h.Name, h.NameRu, h.Description, h.SafeDescription, h.Source, h.OwnerUserId != null,
            h.Requirement, h.ActivationCost, h.Activation, h.Duration, h.Frequency, h.Notes,
            h.Upgrades.OrderBy(u => u.Level)
                .Select(u => new HeroicAbilityUpgradeDto((int)u.Level, u.Cost, u.Description, u.Notes))
                .ToList(),
            h.Effects.Select(e => new RuleEffectDto(e.Kind, e.Amount, e.Duration, e.Description)).ToList());
}
