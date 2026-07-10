using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>Сборка полного DTO листа персонажа из доменной модели.</summary>
public static class SheetBuilder
{
    public static async Task<CharacterSheetDto> BuildAsync(
        IAppDbContext db, Guid userId, Character c, CancellationToken ct = default)
    {
        var ch = c.Characteristics;

        var talentInputs = c.Talents.Select(t => new TalentInput(
            t.TalentDef!.Name, t.TalentDef.Tier, t.Ranks,
            t.TalentDef.WoundBonus, t.TalentDef.StrainBonus, t.TalentDef.SoakBonus,
            t.TalentDef.MeleeDefenseBonus, t.TalentDef.RangedDefenseBonus)).ToList();

        var itemInputs = c.Items.Select(i => new ItemInput(
            i.ItemDef!.Name, i.ItemDef.Kind, i.State, i.ItemDef.Encumbrance, i.Quantity,
            i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense, i.ItemDef.RangedDefense,
            i.ItemDef.EncumbranceThresholdBonus)).ToList();

        var derived = SheetCalculator.ComputeDerived(
            ch, c.Archetype!.WoundBase, c.Archetype.StrainBase, talentInputs, itemInputs);

        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(db, userId, c.System, c.Id, ct: ct);

        // Все навыки системы (встроенные + видимые кастомные владельца), объединённые со строками персонажа.
        var systemSkills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == c.System
                && (s.OwnerUserId == null
                    || (s.OwnerUserId == userId
                        && (s.HomebrewPackId == null || visiblePackIds.Contains(s.HomebrewPackId.Value)))))
            .OrderBy(s => s.Kind).ThenBy(s => s.NameRu)
            .ToListAsync(ct);
        var rows = c.Skills.ToDictionary(s => s.SkillDefId);
        var skills = systemSkills.Select(def =>
        {
            rows.TryGetValue(def.Id, out var row);
            var ranks = row?.Ranks ?? 0;
            var isCareer = row?.IsCareer ?? c.Career!.CareerSkillNames.Contains(def.Name);
            var pool = GenesysRules.BuildDicePool(ch.Get(def.Characteristic), ranks);
            return new CharacterSkillDto(def.Id, def.Name, def.NameRu, def.Kind, def.Characteristic, ranks, isCareer,
                new DicePoolDto(pool.Ability, pool.Proficiency),
                ranks < GenesysRules.MaxSkillRank ? GenesysRules.SkillRankCost(ranks + 1, isCareer) : 0,
                row?.FreeRanks ?? 0);
        }).ToList();

        return new CharacterSheetDto(
            c.Id, c.Name, c.System,
            c.Archetype.ToDto(),
            c.Career!.ToDto(),
            new Dictionary<string, int>
            {
                ["brawn"] = ch.Brawn, ["agility"] = ch.Agility, ["intellect"] = ch.Intellect,
                ["cunning"] = ch.Cunning, ["willpower"] = ch.Willpower, ["presence"] = ch.Presence,
            },
            c.TotalXp, c.SpentXp, c.AvailableXp, c.IsCreationPhase,
            c.WoundsCurrent, c.StrainCurrent, c.Money,
            new DerivedDto(derived.WoundThreshold, derived.StrainThreshold, derived.Soak, derived.MeleeDefense,
                derived.RangedDefense, derived.EncumbranceThreshold, derived.EncumbranceLoad, derived.Encumbered),
            skills,
            c.Talents
                .OrderBy(t => t.TalentDef!.Tier).ThenBy(t => t.TalentDef!.Name)
                .Select(t => new CharacterTalentDto(t.TalentDefId, t.TalentDef!.Name, t.TalentDef.NameRu, t.TalentDef.Tier,
                    t.TalentDef.IsRanked, t.Ranks, t.TalentDef.Activation, t.TalentDef.Description,
                    t.TalentDef.WoundBonus, t.TalentDef.StrainBonus, t.TalentDef.SoakBonus,
                    t.TalentDef.MeleeDefenseBonus, t.TalentDef.RangedDefenseBonus,
                    t.TalentDef.GrantsCharacteristic, t.ParseGrants()))
                .ToList(),
            TalentTierCounter.Count(c.Talents),
            c.HeroicAbility?.ToDto(),
            c.HeroicUpgradeRank,
            c.HeroicUpgradePointsTotal,
            c.HeroicUpgradePointsSpent,
            c.Items
                .OrderBy(i => i.ItemDef!.Kind).ThenBy(i => i.ItemDef!.NameRu)
                .Select(i => new CharacterItemDto(i.Id, i.ItemDefId, i.ItemDef!.Name, i.ItemDef.NameRu, i.ItemDef.Kind, i.State,
                    i.Quantity, i.ItemDef.Encumbrance, i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense,
                    i.ItemDef.RangedDefense, i.ItemDef.EncumbranceThresholdBonus,
                    SheetCalculator.ItemLoad(new ItemInput(i.ItemDef.Name, i.ItemDef.Kind, i.State,
                        i.ItemDef.Encumbrance, i.Quantity)),
                    i.ItemDef.Description, i.ItemDef.Price,
                    i.ItemDef.SkillName, i.ItemDef.Damage, i.ItemDef.Crit, i.ItemDef.RangeBand, i.ItemDef.Properties))
                .ToList(),
            c.Desire, c.Fear, c.Strength, c.Flaw, c.Background,
            c.CriticalInjuries
                .OrderBy(ci => ci.RollResult ?? int.MaxValue).ThenBy(ci => ci.CreatedAt)
                .Select(ci => new CharacterCriticalInjuryDto(
                    ci.Id, ci.RuleCode, ci.NameRu, ci.Severity, ci.RollResult, ci.Notes))
                .ToList(),
            c.PortraitUrl);
    }
}
