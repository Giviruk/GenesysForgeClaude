using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>Сборка полного DTO листа персонажа из доменной модели.</summary>
public static class SheetBuilder
{
    public static async Task<CharacterSheetDto> BuildAsync(
        IAppDbContext db, Guid userId, Character c, CancellationToken ct = default)
    {
        var ch = c.Characteristics;

        var derived = CharacterDerived.Compute(c);

        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(db, userId, c.System, c.Id, ct: ct);

        // Все навыки системы (встроенные + видимые кастомные владельца), объединённые со строками персонажа.
        // Retired-навык не показывается всем подряд, но у персонажа с уже купленными рангами
        // он обязан остаться на листе — иначе ранги просто исчезли бы (ROT-CLEAN-3.2).
        var ownedSkillIds = c.Skills.Select(s => s.SkillDefId).ToList();
        var systemSkills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == c.System
                && (!s.Retired || ownedSkillIds.Contains(s.Id))
                && (s.OwnerUserId == null
                    || (s.OwnerUserId == userId
                        && (s.HomebrewPackId == null || visiblePackIds.Contains(s.HomebrewPackId.Value)))))
            .OrderBy(s => s.Kind).ThenBy(s => s.NameRu)
            .ToListAsync(ct);
        // Карьерный статус — только из резолвера (карьера ∪ вид ∪ таланты); хранимый флаг строки не
        // является источником истины и может отставать от текущего набора талантов.
        var careerSkills = CareerSkills.Resolve(c, c.Career!, systemSkills);
        var rows = c.Skills.ToDictionary(s => s.SkillDefId);
        var skills = systemSkills.Select(def =>
        {
            rows.TryGetValue(def.Id, out var row);
            var ranks = row?.Ranks ?? 0;
            var isCareer = careerSkills.IsCareer(def.Id);
            var pool = GenesysRules.BuildDicePool(ch.Get(def.Characteristic), ranks);
            return new CharacterSkillDto(def.Id, def.Name, def.NameRu, def.Kind, def.Characteristic, ranks, isCareer,
                new DicePoolDto(pool.Ability, pool.Proficiency),
                ranks < GenesysRules.MaxSkillRank ? GenesysRules.SkillRankCost(ranks + 1, isCareer) : 0,
                row?.FreeRanks ?? 0,
                careerSkills.GrantsFor(def.Id)
                    .Select(g => new CareerSkillSourceDto(g.Source.ToString(), g.SourceName))
                    .ToList(),
                def.Retired);
        }).ToList();

        var configuration = await BuildConfigurationAsync(db, c, systemSkills, ct);

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
                    t.TalentDef.GrantsCharacteristic, t.ParseGrants(), t.TalentDef.DescriptionEn,
                    t.Choices.OrderBy(x => x.RankIndex).ThenBy(x => x.Value, StringComparer.Ordinal)
                        .Select(x => new CharacterTalentChoiceDto(x.RankIndex, x.Kind, x.Value, x.DisplayName))
                        .ToList(),
                    t.NeedsChoice, t.TalentDef.ActivationEn, t.TalentDef.CanUseOutOfTurn))
                .ToList(),
            TalentTierCounter.Count(c.Talents),
            c.HeroicAbility?.ToDto(),
            c.HeroicUpgradeRank,
            c.HeroicUpgradePointsTotal,
            c.HeroicUpgradePointsSpent,
            new HeroicUpgradeStateDto(
                c.HeroicUpgradeRank,
                c.HeroicDurationRanks,
                c.HeroicFrequencyRanks,
                c.HeroicStoryUpgrade,
                c.HeroicSecondaryEffects
                    .Where(x => x.HeroicSecondaryEffectDef is not null)
                    .Select(x => x.HeroicSecondaryEffectDef!.ToDto())
                    .OrderBy(x => x.NameRu)
                    .ToList()),
            c.Items
                .OrderBy(i => i.ItemDef!.Kind).ThenBy(i => i.ItemDef!.NameRu)
                .Select(i => new CharacterItemDto(i.Id, i.ItemDefId, i.ItemDef!.Name, i.ItemDef.NameRu, i.ItemDef.Kind, i.State,
                    i.Quantity, i.ItemDef.Encumbrance, i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense,
                    i.ItemDef.RangedDefense, i.ItemDef.EncumbranceThresholdBonus,
                    SheetCalculator.ItemLoad(new ItemInput(i.ItemDef.Name, i.ItemDef.Kind, i.State,
                        i.ItemDef.Encumbrance, i.Quantity)),
                    i.ItemDef.Description, i.ItemDef.Price,
                    i.ItemDef.SkillName, i.ItemDef.Damage, i.ItemDef.Crit, i.ItemDef.RangeBand, i.ItemDef.Properties,
                    i.ItemDef.DescriptionEn))
                .ToList(),
            c.Desire, c.Fear, c.Strength, c.Flaw, c.Background,
            c.CriticalInjuries
                .OrderBy(ci => ci.RollResult ?? int.MaxValue).ThenBy(ci => ci.CreatedAt)
                .Select(ci => new CharacterCriticalInjuryDto(
                    ci.Id, ci.RuleCode, ci.NameRu, ci.Severity, ci.RollResult, ci.Notes))
                .ToList(),
            c.PortraitUrl,
            c.StartingEquipmentMode,
            c.StartingPurchaseBudget,
            c.ThresholdSnapshotProvenance,
            c.RulesReviewRequired,
            c.SpeciesAbilityChoiceCode,
            SpeciesAbilityRules.ChoiceIncomplete(c.Archetype, c.SpeciesAbilityChoiceCode),
            CharacterDerived.Silhouette(c),
            c.HeroicAbilityId is null ? null : new HeroicIdentityDto(
                c.HeroicCustomName,
                c.HeroicOriginMode,
                c.HeroicOriginPrimary,
                c.HeroicOriginSecondary,
                c.HeroicOriginNarrative,
                [.. HeroicIdentityRules.ParseRolls(c.HeroicOriginRolls)],
                c.HeroicIdentityComplete),
            c.HeroicIdentityIncomplete,
            configuration,
            c.HeroicConfigurationIncomplete);
    }

    /// <summary>
    /// Параметр primary effect (ROT-HA-02). Числа именного оружия строятся из профиля, а качества
    /// резолвятся по кодам из справочника — в базе они не дублируются.
    /// </summary>
    private static async Task<HeroicConfigurationDto?> BuildConfigurationAsync(
        IAppDbContext db, Character c, List<SkillDef> visibleSkills, CancellationToken ct)
    {
        if (c.HeroicAbilityId is null) return null;
        var kind = c.RequiredHeroicParameter;
        var config = c.HeroicConfiguration;

        SignatureWeaponDto? weapon = null;
        if (c.SignatureWeapon is { } w)
        {
            var spec = SignatureWeaponProfiles.Get(w.Profile);
            var codes = spec.Qualities.Select(q => q.Code).ToList();
            var defs = await db.QualityDefs.AsNoTracking()
                .Where(q => codes.Contains(q.Code)).ToListAsync(ct);
            var byCode = defs.ToDictionary(q => q.Code, StringComparer.Ordinal);
            weapon = new SignatureWeaponDto(
                w.Profile, w.Craftsmanship, w.NarrativeForm, w.FormTraits, w.IsLost,
                spec.SkillName, spec.Damage, spec.Crit, spec.RangeBand, spec.Encumbrance, spec.HardPoints,
                [.. spec.Qualities.Select(q => byCode.TryGetValue(q.Code, out var def)
                    ? new ItemQualityRefDto(def.Code, def.NameRu, def.NameEn,
                        q.Rating > 0 ? q.Rating : null, def.HasRating, def.IsActive, def.ActivationCost)
                    : new ItemQualityRefDto(q.Code, q.Code, q.Code,
                        q.Rating > 0 ? q.Rating : null, q.Rating > 0, false, ""))]);
        }

        // Скрытый позднее кастомный навык не подменяется другим: остаётся снимок имени и предупреждение.
        var paragonMissing = kind == HeroicParameterKind.ParagonSkill
            && config?.ParagonSkillDefId is { } skillId
            && visibleSkills.TrueForAll(s => s.Id != skillId);

        return new HeroicConfigurationDto(
            kind,
            config?.ParagonSkillDefId,
            string.IsNullOrEmpty(config?.ParagonSkillName) ? null : config.ParagonSkillName,
            paragonMissing,
            string.IsNullOrEmpty(config?.SixthSenseSubject) ? null : config.SixthSenseSubject,
            weapon,
            c.HeroicParameterComplete);
    }
}
