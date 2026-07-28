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
        // Помехи от снаряжения и перегруза считаются один раз на персонажа и раскладываются
        // по каждой проверке (ROT-ARM-01): их же роллер подставляет в пул.
        var checkModifiers = CharacterDerived.CheckModifierInputs(c);
        // Предметы со всеми поправками считаются один раз: и для навыков, и для карточек (ROT-EQP-ATT-01).
        var effectiveItems = EffectiveItems.For(c);
        var skillBoosts = EffectiveItems.SkillBoosts(effectiveItems);
        // Чем персонаж может заплатить за материалы ремонта (GEN-EQP-DMG-01): в фазе создания
        // сначала тратится бюджет стартовых покупок, поэтому он входит в доступную сумму.
        var availableFunds = c.Money + (c.IsCreationPhase ? c.StartingPurchaseBudget : 0);
        var skills = systemSkills.Select(def =>
        {
            rows.TryGetValue(def.Id, out var row);
            var ranks = row?.Ranks ?? 0;
            var isCareer = careerSkills.IsCareer(def.Id);
            var pool = GenesysRules.BuildDicePool(ch.Get(def.Characteristic), ranks);
            var penalty = CheckModifierAggregator.For(
                def.Name, def.Characteristic, checkModifiers, derived.Encumbrance, skillBoosts);
            return new CharacterSkillDto(def.Id, def.Name, def.NameRu, def.Kind, def.Characteristic, ranks, isCareer,
                new DicePoolDto(pool.Ability, pool.Proficiency),
                ranks < GenesysRules.MaxSkillRank ? GenesysRules.SkillRankCost(ranks + 1, isCareer) : 0,
                row?.FreeRanks ?? 0,
                careerSkills.GrantsFor(def.Id)
                    .Select(g => new CareerSkillSourceDto(g.Source.ToString(), g.SourceName))
                    .ToList(),
                def.Retired,
                penalty.SetbackDice,
                [.. penalty.Sources.Select(s => new CheckModifierSourceDto(
                    s.SourceType, s.SourceName, s.SourceNameRu, s.Setback, s.Condition, s.Boost))],
                penalty.BoostDice);
        }).ToList();

        var configuration = await BuildConfigurationAsync(db, c, systemSkills, ct);

        // Качества альтернативных профилей хранятся кодами (ROT-WPN-01) — справочник резолвится один раз.
        var qualitiesByCode = (await db.QualityDefs.AsNoTracking().ToListAsync(ct))
            .ToDictionary(q => q.Code, StringComparer.Ordinal);

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
                derived.RangedDefense, derived.EncumbranceThreshold, derived.EncumbranceLoad, derived.Encumbered,
                ToDto(derived.MeleeDefenseBreakdown), ToDto(derived.RangedDefenseBreakdown),
                derived.Encumbrance is null ? null : new EncumbranceDto(
                    derived.Encumbrance.Overload, derived.Encumbrance.SetbackDice,
                    derived.Encumbrance.HasFreeManoeuvre, derived.Encumbrance.StrainPerManoeuvre,
                    derived.Encumbrance.ZeroEncumbranceLoad)),
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
            effectiveItems
                .OrderBy(e => e.Def.Kind).ThenBy(e => e.Def.NameRu)
                .Select(e => ItemDto(e, ch, qualitiesByCode, availableFunds))
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
            c.HeroicConfigurationIncomplete,
            c.ActiveArmorCharacterItemId,
            [.. c.Attachments.Where(a => a.AttachmentDef is not null)
                .OrderBy(a => a.AttachmentDef!.NameRu, StringComparer.Ordinal)
                .Select(a => AttachmentDto(a, availableFunds))]);
    }

    /// <summary>
    /// Позиция инвентаря на листе. Все числа — уже эффективные для экземпляра (ROT-WPN-02):
    /// каталожные значения остаются видимыми в разборе поправок, а не в самих полях, иначе
    /// у железной кольчуги на карточке стоял бы один вес, а в переносимом грузе — другой.
    /// </summary>
    private static CharacterItemDto ItemDto(
        EffectiveItem e, CharacteristicsSet ch,
        IReadOnlyDictionary<string, QualityDef> qualitiesByCode, int availableFunds)
    {
        var (item, def) = (e.Item, e.Def);
        return new CharacterItemDto(
            item.Id, item.ItemDefId, def.Name, def.NameRu, def.Kind, item.State,
            item.Quantity, e.Encumbrance, e.SoakBonus, e.MeleeDefense,
            e.RangedDefense, e.EncumbranceThresholdBonus,
            SheetCalculator.ItemLoad(new ItemInput(def.Name, def.Kind, item.State,
                e.Encumbrance, item.Quantity)),
            def.Description, e.Price,
            def.SkillName, def.Damage, def.Crit, def.RangeBand, def.Properties,
            def.DescriptionEn,
            e.IsActiveArmor,
            e.HardPoints,
            [.. def.CheckModifiers.Select(m => new ItemCheckModifierDto(
                    m.Kind, m.SkillName, m.Characteristic, m.Value, m.RequiresWorn, m.Condition))
                .Concat(e.CheckModifiers.Select(m => new ItemCheckModifierDto(
                    m.Kind, m.SkillName, null, m.Value, true, "")))],
            def.AttackProfileDtos(ch.Brawn, qualitiesByCode, ch.Agility, item.Craftsmanship,
                e.Qualities, e.AttachmentEffects.DamageBonus, e.AttachmentEffects.CritReduction,
                item.DamageState),
            item.IsThrown,
            item.Craftsmanship,
            e.Rarity,
            e.Reinforced,
            [.. e.Adjustments.Select(a => new ItemStatAdjustmentDto(
                a.Field, a.Base, a.Effective, a.Stage, a.Source))],
            [.. e.Attachments.Select(a => AttachmentDto(a, availableFunds))],
            e.UsedHardPoints,
            e.OverCapacity,
            [.. e.AttachmentEffects.Notes.Select(n => $"{n.SourceNameRu}: {n.Text}")],
            def.FormTraits,
            item.DamageState,
            e.IsUsable,
            RepairDto(e.Price, item.DamageState, availableFunds),
            ImplementDto(e));
    }

    /// <summary>
    /// Магический инструмент в DTO (ROT-MAG-IMP-01). Механика приезжает вместе с позицией: сборщик
    /// заклинаний должен знать, какой эффект инструмент удешевляет, не спрашивая справочник заново.
    /// </summary>
    private static ItemImplementDto? ImplementDto(EffectiveItem e)
    {
        if (e.Implement is not { } spec) return null;
        var chosen = e.Item.ImplementChoices
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new ItemImplementDto(
            spec.Code, e.Item.ImplementMaterial, spec.AttackDamageBonus, spec.BoostDice,
            spec.RequiredMagicSkill, spec.Discount, spec.DiscountEffects, spec.ChoiceCount,
            spec.ChoiceMaxIncreaseSum, spec.ChoiceExactIncrease, chosen,
            spec.NeedsConfiguration && !e.Item.ImplementConfigured);
    }

    /// <summary>Экземпляр улучшения в DTO: механика приезжает вместе с ним, а не отдельным запросом.</summary>
    internal static CharacterAttachmentDto AttachmentDto(CharacterAttachment a, int availableFunds) => new(
        a.Id, a.AttachmentDefId, a.AttachmentDef!.Name, a.AttachmentDef.NameRu,
        a.AttachmentDef.HardPointCost, a.AttachmentDef.IsEnchantment, a.AttachmentDef.Price,
        a.AttachmentDef.Rarity, a.HostCharacterItemId, a.Note,
        [.. a.AttachmentDef.Effects.Select(Mappers.ToDto)],
        a.DamageState,
        DamageStateRules.IsUsable(a.DamageState),
        RepairDto(a.AttachmentDef.Price, a.DamageState, availableFunds));

    /// <summary>
    /// Памятка по ремонту экземпляра (GEN-EQP-DMG-01). Отдаётся всегда, даже целому предмету:
    /// правило, время и доля материалов — это справка, которую игрок должен видеть до того, как
    /// вещь сломается. <c>null</c> у цены означает «обычной цены нет» — сумму называет ведущий.
    /// </summary>
    private static ItemRepairDto RepairDto(int? instancePrice, ItemDamageState state, int availableFunds)
    {
        var estimate = DamageStateRules.Estimate(instancePrice ?? 0, state);
        int? cost = instancePrice is null && estimate.CanRepair ? null : estimate.MaterialCost;
        return new ItemRepairDto(
            state, estimate.CanRepair, estimate.Difficulty, estimate.HoursMin, estimate.HoursMax,
            estimate.MaterialPercent, cost, DamageStateRules.DefaultSkillName,
            cost is not null && cost <= availableFunds);
    }

    /// <summary>Разбор защиты в DTO: источники нужны UI, чтобы объяснить итоговое число.</summary>
    private static DefenseBreakdownDto? ToDto(DefenseBreakdown? b) => b is null ? null : new(
        b.Raw, b.Effective, b.Capped,
        b.Provider is null ? null : Source(b.Provider),
        [.. b.IgnoredProviders.Select(Source)],
        [.. b.Increases.Select(Source)]);

    private static DefenseSourceDto Source(DefenseContribution c) => new(c.SourceType, c.SourceName, c.Value);

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
            // Качество изготовления именного оружия наконец считается, а не просто хранится
            // (ROT-WPN-02): это то же оружие, что и в инвентаре, и правила у него те же.
            var stats = CraftsmanshipRules.For(
                ItemKind.Weapon, w.Craftsmanship, spec.Encumbrance, 0, 0, 0, spec.HardPoints, 0, 0);
            var damageValue = spec.DamageValue + CraftsmanshipRules.DamageDelta(w.Craftsmanship);
            // Древняя работа добавляет Укреплённое — тем же кодом справочника, что и у предметов.
            List<(string Code, int Rating)> qualities = stats.Reinforced
                ? [.. spec.Qualities, (CraftsmanshipRules.ReinforcedQualityCode, 0)]
                : [.. spec.Qualities];
            var codes = qualities.Select(q => q.Code).ToList();
            var defs = await db.QualityDefs.AsNoTracking()
                .Where(q => codes.Contains(q.Code)).ToListAsync(ct);
            var byCode = defs.ToDictionary(q => q.Code, StringComparer.Ordinal);
            weapon = new SignatureWeaponDto(
                w.Profile, w.Craftsmanship, w.NarrativeForm, w.FormTraits, w.IsLost,
                spec.SkillName,
                SignatureWeaponProfileSpec.DamageText(spec.DamageKind, damageValue),
                CraftsmanshipRules.Crit(spec.Crit, w.Craftsmanship), spec.RangeBand,
                stats.Encumbrance, stats.HardPoints ?? spec.HardPoints,
                [.. qualities.Select(q => byCode.TryGetValue(q.Code, out var def)
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
