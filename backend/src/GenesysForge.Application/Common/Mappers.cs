using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

public static class Mappers
{
    public static ArchetypeDto ToDto(this ArchetypeDef a) => new(a.Id, a.Name, a.NameRu, a.Brawn, a.Agility,
        a.Intellect, a.Cunning, a.Willpower, a.Presence, a.WoundBase, a.StrainBase, a.StartingXp,
        a.Description, a.SafeDescription, a.Source, a.OwnerUserId != null,
        a.Abilities.Select(x => new ArchetypeAbilityDto(x.Code, x.NameRu, x.NameEn, x.SafeDescription, x.AutomationKind, x.DescriptionEn,
            x.RuleKind, x.RuleValue, x.RuleParameters, x.UsesPerScope, x.UseScope, x.StoryPointCost,
            SpeciesAbilityRules.ChoiceOptions(x))).ToList(),
        a.StartingSkills.Select(x => new ArchetypeStartingSkillDto(x.SkillName, x.NameRu, x.FreeRanks, x.IsChoice, x.ChoiceGroup, x.ChoiceCount, x.GrantsCareerSkill)).ToList(),
        a.DescriptionEn,
        a.Silhouette);

    public static CareerDto ToDto(this CareerDef c) =>
        new(c.Id, c.Name, c.NameRu, c.Description, c.SafeDescription, c.Source, c.OwnerUserId != null, c.CareerSkillNames,
            c.StartingMoneyFixed, c.StartingMoneyDice,
            c.StartingGear.Select(g => new CareerStartingGearDto(g.ItemCode, g.ItemNameFallback, g.Quantity,
                g.IsChoice, g.ChoiceGroup, g.ChoiceOption)).ToList(),
            c.Rules.Select(r => new CareerRuleDto(r.Code, r.Kind, r.Description, r.DescriptionEn)).ToList(),
            c.DescriptionEn);

    public static SkillDefDto ToDto(this SkillDef s) =>
        new(s.Id, s.Name, s.NameRu, s.Characteristic, s.Kind, s.SafeDescription, s.Source, s.OwnerUserId != null, s.DescriptionEn);

    public static TalentDefDto ToDto(this TalentDef t) => new(t.Id, t.Name, t.NameRu, t.Tier, t.IsRanked, t.Category, t.Setting,
        t.Activation, t.Description, t.SafeDescription, t.Source,
        t.WoundBonus, t.StrainBonus, t.SoakBonus, t.MeleeDefenseBonus, t.RangedDefenseBonus, t.OwnerUserId != null,
        t.GrantsCharacteristic, t.DescriptionEn,
        t.ActivationEn, t.CanUseOutOfTurn, t.CareerSkillNames,
        TalentPurchasePolicy.BareCode(t.Code), t.RequiresTalentCode, t.ExcludesTalentCodes,
        t.UsesPerScope, t.UseScope, t.StoryPointCost, t.StrainCost, t.Trigger,
        TalentChoiceSchemas.For(t).Kind,
        TalentChoiceSchemas.For(t).CountForFirstRank,
        TalentChoiceSchemas.For(t).CountForNextRank);

    /// <param name="qualitiesByCode">
    /// Справочник качеств для альтернативных профилей атаки (ROT-WPN-01): они хранятся кодами.
    /// Без справочника вместо названия останется код — выдумывать имя нельзя.
    /// </param>
    public static ItemDefDto ToDto(this ItemDef i, IReadOnlyDictionary<string, QualityDef>? qualitiesByCode = null)
        => new(i.Id, i.Name, i.NameRu, i.Kind, i.Encumbrance, i.SoakBonus,
        i.MeleeDefense, i.RangedDefense, i.EncumbranceThresholdBonus,
        i.Description, i.SafeDescription, i.Source, i.Price, i.Rarity,
        i.SkillName, i.Damage, i.Crit, i.RangeBand, i.Properties, i.OwnerUserId != null,
        i.Qualities
            .Where(q => q.QualityDef != null)
            .Select(q => new ItemQualityRefDto(
                q.QualityDef!.Code, q.QualityDef.NameRu, q.QualityDef.NameEn, q.Rating,
                q.QualityDef.HasRating, q.QualityDef.IsActive, q.QualityDef.ActivationCost))
            .ToList(), i.DescriptionEn,
        i.HardPoints,
        [.. i.CheckModifiers.Select(m => new ItemCheckModifierDto(
            m.Kind, m.SkillName, m.Characteristic, m.Value, m.RequiresWorn, m.Condition))],
        i.AttackProfileDtos(qualitiesByCode: qualitiesByCode),
        ImplementSpecDto(i.Code),
        RuneboundShardSpecDto(i.Code),
        i.Purchasable,
        i.Sellable,
        i.Code,
        ShopCatalogRules.Category(i));

    /// <summary>
    /// Паспорт магического инструмента для справочника (ROT-MAG-IMP-01). Определяется кодом
    /// записи, а не разбором названия: «Посох света» — оружие, а не магический посох.
    /// </summary>
    public static ImplementSpecDto? ImplementSpecDto(string? code) =>
        ImplementRules.For(code) is not { } spec
            ? null
            : new ImplementSpecDto(
                spec.Code, spec.AttackDamageBonus, spec.BoostDice, spec.RequiredMagicSkill,
                spec.Discount, spec.DiscountEffects, spec.ChoiceCount,
                spec.ChoiceMaxIncreaseSum, spec.ChoiceExactIncrease);

    public static RuneboundShardSpecDto? RuneboundShardSpecDto(string? code) =>
        RuneboundShardRules.For(code) is not { } spec
            ? null
            : new RuneboundShardSpecDto(
                spec.Code,
                RuneboundShardRules.RequiredMagicSkill,
                RuneboundShardRules.MinimumSkillRank,
                spec.AttackDamageBonus,
                spec.CastingStrainReduction,
                [.. spec.DifficultyReductions.Select(r =>
                    new ShardDifficultyReductionDto(r.Action, r.Amount))],
                [.. spec.SpellEffects.Select(e =>
                    new ShardSpellEffectDto(
                        e.Action, e.EffectCode, e.Mode, e.FreeUses,
                        e.OverridesSkillRestriction))],
                spec.ActivationCost,
                spec.ActivationFrequency,
                spec.ActivationAttack is null
                    ? null
                    : new ShardActivationAttackDto(
                        spec.ActivationAttack.Skill,
                        spec.ActivationAttack.Damage,
                        spec.ActivationAttack.Critical,
                        spec.ActivationAttack.Range,
                        [.. spec.ActivationAttack.Qualities.Select(q =>
                            new ShardActivationQualityDto(q.Code, q.Rating))]),
                spec.NeedsConfiguration);

    /// <summary>Улучшение справочника в DTO: механика приезжает типизированной, а не текстом.</summary>
    public static AttachmentDefDto ToDto(this AttachmentDef a) => new(
        a.Id, a.Code, a.Name, a.NameRu, a.HardPointCost, a.Price, a.Rarity, a.IsEnchantment,
        a.HostKind, a.RequiredTraits, a.RequiredAnyTraits, a.ForbiddenTraits,
        a.SafeDescription, a.DescriptionEn, a.Source,
        [.. a.Effects.Select(ToDto)]);

    /// <summary>
    /// Один эффект улучшения. <c>Executed</c> честно показывает, считает ли его приложение:
    /// автоматические символы и эффекты, которым нужен рантайм столкновения, только описаны.
    /// </summary>
    public static AttachmentEffectDto ToDto(this AttachmentEffect e) => new(
        e.Kind, e.QualityCode, e.SkillName, e.Value, e.Increment, e.Condition, e.Note,
        e.Kind is not (AttachmentEffectKind.NarrativeOnly or AttachmentEffectKind.AutomaticSymbol));

    /// <summary>
    /// Профили атаки предмета (ROT-WPN-01). Профиль по умолчанию показывает качества самого
    /// предмета — в базе они не дублируются; у альтернативных профилей качества свои.
    /// </summary>
    /// <param name="brawn">Мощь персонажа: с ней считается базовый урон. <c>null</c> — справочник.</param>
    /// <param name="qualitiesByCode">Справочник качеств для расшифровки кодов альтернативных профилей.</param>
    /// <param name="agility">
    /// Ловкость персонажа: вместе с Мощью нужна для Громоздкого и Сноровки (GEN-EQP-QUAL-01).
    /// </param>
    /// <param name="craftsmanship">
    /// Качество изготовления экземпляра (ROT-WPN-02): урон и крит профиля приезжают уже с его
    /// поправками. В справочнике — <see cref="WeaponCraftsmanship.Steel"/>: у записи каталога
    /// качества изготовления нет, оно бывает только у экземпляра.
    /// </param>
    /// <param name="effectiveQualities">
    /// Итоговые качества экземпляра с учётом улучшений (ROT-EQP-ATT-01). <c>null</c> — берутся
    /// качества самой записи каталога: в справочнике улучшений нет.
    /// </param>
    /// <param name="damageBonus">Прибавка урона от улучшений; применяется после качества изготовления.</param>
    /// <param name="critReduction">Уменьшение крита от улучшений; итог не ниже единицы.</param>
    /// <param name="damageState">
    /// Состояние повреждения экземпляра (GEN-EQP-DMG-01): Незначительное добавляет в пул помеху,
    /// Умеренное поднимает сложность. В справочнике — <see cref="ItemDamageState.Undamaged"/>:
    /// повреждена бывает вещь, а не строка каталога.
    /// </param>
    public static List<WeaponAttackProfileDto> AttackProfileDtos(
        this ItemDef i, int? brawn = null, IReadOnlyDictionary<string, QualityDef>? qualitiesByCode = null,
        int? agility = null, WeaponCraftsmanship craftsmanship = WeaponCraftsmanship.Steel,
        IReadOnlyList<EffectiveQuality>? effectiveQualities = null,
        int damageBonus = 0, int critReduction = 0,
        ItemDamageState damageState = ItemDamageState.Undamaged)
    {
        // Качества экземпляра: база предмета плюс то, что дали улучшения. Название и механика
        // берутся из справочника по коду — выдумывать их нельзя.
        var qualityPairs = effectiveQualities is null
            ? [.. i.Qualities.Where(q => q.QualityDef != null)
                .Select(q => (Def: q.QualityDef!, Rating: q.Rating))]
            : effectiveQualities
                .Where(q => qualitiesByCode is not null && qualitiesByCode.ContainsKey(q.Code))
                .Select(q => (Def: qualitiesByCode![q.Code], Rating: q.Rating > 0 ? q.Rating : (int?)null))
                .ToList();

        var itemQualities = qualityPairs
            .Select(q => new ItemQualityRefDto(
                q.Def.Code, q.Def.NameRu, q.Def.NameEn, q.Rating,
                q.Def.HasRating, q.Def.IsActive, q.Def.ActivationCost))
            .ToList();

        // Механика качеств профиля по умолчанию берётся из самого предмета, у альтернативного —
        // из справочника по коду. Без справочника механики нет: угадывать её по коду нельзя.
        var itemEffects = qualityPairs
            .Select(q => new WeaponQualityInput(
                q.Def.NameEn, q.Def.NameRu, q.Def.EffectKind, q.Rating ?? 0))
            .ToList();

        List<WeaponQualityInput> ProfileEffects(WeaponAttackProfile p) => p.IsDefault
            ? itemEffects
            : [.. p.Qualities
                .Where(q => qualitiesByCode is not null && qualitiesByCode.ContainsKey(q.Code))
                .Select(q => new WeaponQualityInput(
                    qualitiesByCode![q.Code].NameEn, qualitiesByCode[q.Code].NameRu,
                    qualitiesByCode[q.Code].EffectKind, q.Rating))];

        // Качество изготовления сдвигает печатную прибавку, а пол урона считается по итогу —
        // тому числу, которое персонаж действительно наносит (ROT-WPN-02).
        var damageDelta = CraftsmanshipRules.DamageDelta(craftsmanship);

        return [.. i.AttackProfiles
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.Code, StringComparer.Ordinal)
            .Select(p => new WeaponAttackProfileDto(
                p.Code, p.NameRu, p.NameEn, p.IsDefault, p.SkillName, p.DamageKind,
                p.DamageValue + damageDelta + damageBonus,
                Math.Max(CraftsmanshipRules.MinCrit,
                    CraftsmanshipRules.Crit(p.Crit, craftsmanship) - critReduction),
                p.Range, p.CannotAttackEngaged, p.FixedDifficulty,
                p.IsDefault ? itemQualities : [.. p.Qualities.Select(q => QualityRef(q, qualitiesByCode))],
                brawn is { } b
                    ? CraftsmanshipRules.Damage(
                        WeaponProfileRules.BaseDamage(p.DamageKind, p.DamageValue, b), craftsmanship)
                        + damageBonus
                    : null,
                brawn is { } b2 && agility is { } a
                    ? PoolDto(DamageStateRules.ApplyTo(
                        WeaponQualityRules.PoolFor(ProfileEffects(p), b2, a), damageState))
                    : null))];
    }

    /// <summary>Разбор изменения пула от качеств — с источниками, чтобы куб был объясним.</summary>
    private static AttackPoolModifiersDto PoolDto(AttackPoolModifiers m) => new(
        m.Boost, m.Setback, m.DifficultyIncrease, m.AutomaticAdvantage, m.AutomaticThreat,
        [.. m.Sources.Select(s => new QualityContributionDto(
            s.NameEn, s.NameRu, s.Boost, s.Setback, s.Difficulty, s.Advantage, s.Threat))]);

    /// <summary>Код качества → запись справочника; без справочника остаётся сам код, а не выдумка.</summary>
    private static ItemQualityRefDto QualityRef(
        WeaponProfileQuality q, IReadOnlyDictionary<string, QualityDef>? byCode) =>
        byCode is not null && byCode.TryGetValue(q.Code, out var def)
            ? new ItemQualityRefDto(def.Code, def.NameRu, def.NameEn,
                q.Rating > 0 ? q.Rating : null, def.HasRating, def.IsActive, def.ActivationCost)
            : new ItemQualityRefDto(q.Code, q.Code, q.Code,
                q.Rating > 0 ? q.Rating : null, q.Rating > 0, false, "");

    public static QualityDto ToDto(this QualityDef q) => new(q.Id, q.Code, q.NameEn, q.NameRu, q.Kind,
        q.IsActive, q.HasRating, q.ActivationCost, q.Category, q.Description, q.SafeDescription, q.Source,
        q.DescriptionEn, q.EffectKind, q.AdvantageCost, q.RequiresHit, q.CanActivateOnMiss, q.TriumphMayPay,
        q.Repeatability);

    public static RuleTableEntryDto ToDto(this RuleTableEntry r) => new(r.Id, r.Kind, r.Code, r.NameRu,
        r.NameEn, r.GroupRu, r.SortOrder, r.RollRange, r.SymbolCost, r.Body, r.Notes, r.Source, r.SourcePage,
        r.GroupEn, r.BodyEn, r.NotesEn);

    public static HeroicAbilityDto ToDto(this HeroicAbilityDef h) =>
        new(h.Id, h.Code, h.Name, h.NameRu, h.Description, h.SafeDescription, h.Source, h.OwnerUserId != null,
            h.Requirement, h.ActivationCost, h.Activation, h.Duration, h.Frequency, h.Notes,
            h.Upgrades.OrderBy(u => u.Level)
                .Select(u => new HeroicAbilityUpgradeDto((int)u.Level, u.Cost, u.Description, u.Notes, u.DescriptionEn))
                .ToList(),
            h.Effects.Select(e => new RuleEffectDto(e.Kind, e.Amount, e.Duration, e.Description)).ToList(),
            h.DescriptionEn);

    public static HeroicSecondaryEffectDto ToDto(this HeroicSecondaryEffectDef x) =>
        new(x.Id, x.Code, x.Name, x.NameRu, x.Description, x.SafeDescription, x.Source, x.DescriptionEn);
}
