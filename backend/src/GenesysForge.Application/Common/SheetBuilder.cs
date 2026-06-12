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

        // Все навыки системы (встроенные + кастомные владельца), объединённые со строками персонажа.
        var systemSkills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == c.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .OrderBy(s => s.Kind).ThenBy(s => s.Name)
            .ToListAsync(ct);
        var rows = c.Skills.ToDictionary(s => s.SkillDefId);
        var skills = systemSkills.Select(def =>
        {
            rows.TryGetValue(def.Id, out var row);
            var ranks = row?.Ranks ?? 0;
            var isCareer = row?.IsCareer ?? c.Career!.CareerSkillNames.Contains(def.Name);
            var pool = GenesysRules.BuildDicePool(ch.Get(def.Characteristic), ranks);
            return new CharacterSkillDto(def.Id, def.Name, def.Kind, def.Characteristic, ranks, isCareer,
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
            c.WoundsCurrent, c.StrainCurrent,
            new DerivedDto(derived.WoundThreshold, derived.StrainThreshold, derived.Soak, derived.MeleeDefense,
                derived.RangedDefense, derived.EncumbranceThreshold, derived.EncumbranceLoad, derived.Encumbered),
            skills,
            c.Talents
                .OrderBy(t => t.TalentDef!.Tier).ThenBy(t => t.TalentDef!.Name)
                .Select(t => new CharacterTalentDto(t.TalentDefId, t.TalentDef!.Name, t.TalentDef.Tier,
                    t.TalentDef.IsRanked, t.Ranks, t.TalentDef.Activation, t.TalentDef.Description))
                .ToList(),
            TalentTierCounter.Count(c.Talents),
            c.HeroicAbility?.ToDto(),
            c.Items
                .OrderBy(i => i.ItemDef!.Kind).ThenBy(i => i.ItemDef!.Name)
                .Select(i => new CharacterItemDto(i.Id, i.ItemDefId, i.ItemDef!.Name, i.ItemDef.Kind, i.State,
                    i.Quantity, i.ItemDef.Encumbrance, i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense,
                    i.ItemDef.RangedDefense, i.ItemDef.EncumbranceThresholdBonus,
                    SheetCalculator.ItemLoad(new ItemInput(i.ItemDef.Name, i.ItemDef.Kind, i.State,
                        i.ItemDef.Encumbrance, i.Quantity)),
                    i.ItemDef.Description))
                .ToList());
    }
}
