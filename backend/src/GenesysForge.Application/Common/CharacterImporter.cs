using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>Разрешённый из файла персонаж (ещё не сохранён) + заметки и предупреждения.</summary>
public record ImportResolution(
    Character Character,
    List<CharacterNote> Notes,
    string ArchetypeName,
    string CareerName,
    List<string> Warnings);

/// <summary>
/// Общая логика разбора файла формата <see cref="CharacterExportDto.CurrentFormat"/> для импорта
/// и предпросмотра. Built-in контент маппится по <c>Code</c> (fallback System+Name), custom — по
/// Name в области видимости владельца. Неразрешённые навыки/таланты/предметы/героика пропускаются
/// с предупреждением; неразрешённые архетип/карьера блокируют импорт.
/// </summary>
public static class CharacterImporter
{
    public static async Task<ImportResolution> ResolveAsync(
        IAppDbContext db, Guid userId, CharacterExportDto? payload, CancellationToken ct = default)
    {
        if (payload is null || !CharacterExportDto.SupportedFormats.Contains(payload.Format))
            throw new DomainRuleException(
                $"Неподдерживаемый формат файла. Поддерживаются: {string.Join(", ", CharacterExportDto.SupportedFormats)}.");
        var data = payload.Character ?? throw new DomainRuleException("В файле нет данных персонажа.");
        if (string.IsNullOrWhiteSpace(data.Name))
            throw new DomainRuleException("Имя персонажа не может быть пустым.");

        var warnings = new List<string>();
        var system = data.System;

        var archetype = await ResolveArchetypeAsync(db, system, data.ArchetypeCode, data.ArchetypeName, ct)
            ?? throw new DomainRuleException(
                $"Не найден архетип «{Display(data.ArchetypeName, data.ArchetypeCode)}» для системы {system}.");
        var career = await ResolveCareerAsync(db, system, data.CareerCode, data.CareerName, ct)
            ?? throw new DomainRuleException(
                $"Не найдена карьера «{Display(data.CareerName, data.CareerCode)}» для системы {system}.");

        var characterId = Guid.NewGuid();
        var character = new Character
        {
            Id = characterId,
            OwnerUserId = userId,
            Name = data.Name.Trim(),
            System = system,
            ArchetypeId = archetype.Id,
            CareerId = career.Id,
            Brawn = Char(data, "brawn", archetype.Brawn),
            Agility = Char(data, "agility", archetype.Agility),
            Intellect = Char(data, "intellect", archetype.Intellect),
            Cunning = Char(data, "cunning", archetype.Cunning),
            Willpower = Char(data, "willpower", archetype.Willpower),
            Presence = Char(data, "presence", archetype.Presence),
            TotalXp = Math.Max(0, data.TotalXp),
            SpentXp = Math.Max(0, data.SpentXp),
            IsCreationPhase = data.IsCreationPhase,
            WoundsCurrent = Math.Max(0, data.WoundsCurrent),
            StrainCurrent = Math.Max(0, data.StrainCurrent),
            Money = Math.Max(0, data.Money),
            SpeciesAbilityChoiceCode = data.SpeciesAbilityChoiceCode ?? "",
            StartingEquipmentMode = data.StartingEquipmentMode,
            // Бюджет создания переносится только пока персонаж не завершил создание.
            StartingPurchaseBudget = data.IsCreationPhase ? Math.Max(0, data.StartingPurchaseBudget) : 0,
            HeroicUpgradeRank = 0,
        };

        foreach (var s in data.Skills ?? [])
        {
            var def = await ResolveSkillAsync(db, userId, system, s.Code, s.Name, ct);
            if (def is null) { warnings.Add($"Навык «{Display(s.Name, s.Code)}» не найден — пропущен."); continue; }
            character.Skills.Add(new CharacterSkill
            {
                Id = Guid.NewGuid(), CharacterId = characterId, SkillDefId = def.Id,
                Ranks = Math.Max(0, s.Ranks), IsCareer = s.IsCareer, FreeRanks = Math.Max(0, s.FreeRanks),
            });
        }

        foreach (var t in data.Talents ?? [])
        {
            var def = await ResolveTalentAsync(db, userId, system, t.Code, t.Name, ct);
            if (def is null) { warnings.Add($"Талант «{Display(t.Name, t.Code)}» не найден — пропущен."); continue; }
            var talentId = Guid.NewGuid();
            var imported = new CharacterTalent
            {
                Id = talentId, CharacterId = characterId, TalentDefId = def.Id,
                Ranks = Math.Max(1, t.Ranks), GrantedCharacteristics = t.GrantedCharacteristics ?? "",
                Choices = (t.Choices ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => new CharacterTalentChoice
                    {
                        Id = Guid.NewGuid(), CharacterTalentId = talentId,
                        RankIndex = Math.Max(0, x.RankIndex), Kind = x.Kind,
                        Value = x.Value, DisplayName = x.DisplayName ?? x.Value,
                    })
                    .ToList(),
            };

            // Файл без выборов у таланта, который их требует, не чинится молча: талант помечается
            // как требующий ручного выбора, XP при этом повторно не списывается (ROT-TAL-03).
            var schema = TalentChoiceSchemas.For(def);
            if (schema.Required && imported.Choices.Count == 0)
            {
                imported.NeedsChoice = LegacyGrantsToChoices(imported, schema) == 0;
                if (imported.NeedsChoice)
                    warnings.Add($"У таланта «{def.Name}» не сохранён обязательный выбор — его нужно указать вручную.");
            }

            character.Talents.Add(imported);
        }

        foreach (var it in data.Items ?? [])
        {
            var def = await ResolveItemAsync(db, userId, system, it.Code, it.Name, ct);
            if (def is null) { warnings.Add($"Предмет «{Display(it.Name, it.Code)}» не найден — пропущен."); continue; }
            // Качество изготовления файла проверяется, а не применяется на веру: снаряжение
            // эльфийским не бывает, и такой файл чинится обычной работой с предупреждением.
            var craftsmanship = it.Craftsmanship;
            if (!Enum.IsDefined(craftsmanship)
                || (craftsmanship != WeaponCraftsmanship.Steel && !CraftsmanshipRules.AppliesTo(def.Kind)))
            {
                warnings.Add($"У предмета «{def.Name}» указано неприменимое качество изготовления — оставлена обычная работа.");
                craftsmanship = WeaponCraftsmanship.Steel;
            }
            character.Items.Add(new CharacterItem
            {
                Id = Guid.NewGuid(), CharacterId = characterId, ItemDefId = def.Id,
                Quantity = Math.Max(1, it.Quantity), State = it.State,
                Craftsmanship = CraftsmanshipRules.FixedFor(def.Code) ?? craftsmanship,
                // Комплект и стартовый бюджет сохраняются как провенанс; всё остальное — Imported,
                // чтобы импорт не выглядел покупкой в истории нового персонажа.
                Provenance = it.Provenance is ItemProvenance.CareerPackage or ItemProvenance.StartingBudget
                    ? it.Provenance
                    : ItemProvenance.Imported,
            });
        }

        if (!string.IsNullOrWhiteSpace(data.HeroicAbilityCode) || !string.IsNullOrWhiteSpace(data.HeroicAbilityName))
        {
            var heroic = await ResolveHeroicAsync(db, userId, data.HeroicAbilityCode, data.HeroicAbilityName, ct);
            if (heroic is null)
                warnings.Add($"Героическая способность «{Display(data.HeroicAbilityName, data.HeroicAbilityCode)}» не найдена — пропущена.");
            else
            {
                character.HeroicAbilityId = heroic.Id;
                // Навигация нужна дальше: требование параметра выводится из кода способности.
                character.HeroicAbility = heroic;
                character.HeroicUpgradeRank = Math.Clamp(data.HeroicUpgradeRank, 0, 2);
                character.HeroicDurationRanks = Math.Max(0, data.HeroicDurationRanks);
                character.HeroicFrequencyRanks = Math.Max(0, data.HeroicFrequencyRanks);
                character.HeroicStoryUpgrade = data.HeroicStoryUpgrade;

                // ROT-HA-01: личность переносится только целиком и только в валидном виде. Файл v1
                // и подделанные поля дают предупреждение — достраивать происхождение за игрока нельзя.
                if (data.HeroicOriginMode is not null)
                {
                    try
                    {
                        var identity = HeroicIdentityRules.Validate(
                            data.HeroicCustomName,
                            data.HeroicOriginMode.Value,
                            data.HeroicOriginPrimary,
                            data.HeroicOriginSecondary,
                            data.HeroicOriginNarrative,
                            [.. (data.HeroicOriginRolls ?? []).Where(f => f is >= 0 and <= 9)]);
                        character.HeroicCustomName = identity.CustomName;
                        character.HeroicOriginMode = identity.OriginMode;
                        character.HeroicOriginPrimary = identity.OriginPrimary;
                        character.HeroicOriginSecondary = identity.OriginSecondary;
                        character.HeroicOriginNarrative = identity.OriginNarrative;
                        character.HeroicOriginRolls = HeroicIdentityRules.FormatRolls(identity.OriginRolls);
                    }
                    catch (DomainRuleException ex)
                    {
                        warnings.Add($"Личность героической способности не перенесена: {ex.Message}");
                    }
                }

                var effectCodes = (data.HeroicSecondaryEffectCodes ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(2).ToList();
                var effectDefs = await db.HeroicSecondaryEffectDefs
                    .Where(x => effectCodes.Contains(x.Code)).ToListAsync(ct);
                foreach (var effect in effectDefs)
                {
                    character.HeroicSecondaryEffects.Add(new CharacterHeroicSecondaryEffect
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = characterId,
                        HeroicSecondaryEffectDefId = effect.Id,
                        HeroicSecondaryEffectDef = effect,
                    });
                }

                var powerCost = heroic.Upgrades.Where(u => (int)u.Level <= character.HeroicUpgradeRank).Sum(u => u.Cost);
                var importedCost = powerCost + character.HeroicDurationRanks + character.HeroicFrequencyRanks * 2
                    + (character.HeroicStoryUpgrade ? 1 : 0) + character.HeroicSecondaryEffects.Count;
                var points = Math.Max(0, character.TotalXp - archetype.StartingXp) / 50;
                if (importedCost > points)
                {
                    warnings.Add("Улучшения героической способности превышают доступные ability points — сброшены.");
                    character.HeroicUpgradeRank = 0;
                    character.HeroicDurationRanks = 0;
                    character.HeroicFrequencyRanks = 0;
                    character.HeroicStoryUpgrade = false;
                    character.HeroicSecondaryEffects.Clear();
                }
            }
        }
        if (character.System == GameSystem.RealmsOfTerrinoth
            && character.HeroicAbilityId is null
            && !character.IsCreationPhase)
        {
            character.IsCreationPhase = true;
            warnings.Add("У персонажа RoT нет героической способности — фаза создания оставлена открытой.");
        }
        // ROT-HA-02: параметр primary effect. Навык Paragon резолвится по коду/имени в области
        // видимости импортирующего; нерезолвленный навык не подменяется другим.
        if (character.HeroicAbilityId is not null)
        {
            var kind = HeroicParameterRules.Required(character.HeroicAbility?.Code);
            if (kind == HeroicParameterKind.ParagonSkill && !string.IsNullOrWhiteSpace(data.ParagonSkillName))
            {
                var skill = await ResolveSkillAsync(
                    db, userId, character.System, data.ParagonSkillCode, data.ParagonSkillName, ct);
                if (skill is null)
                    warnings.Add($"Навык Paragon «{data.ParagonSkillName}» не найден — выберите его заново.");
                else
                    character.HeroicConfiguration = new CharacterHeroicConfiguration
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = characterId,
                        ParagonSkillDefId = skill.Id,
                        ParagonSkillName = skill.Name,
                    };
            }
            else if (kind == HeroicParameterKind.SixthSenseSubject
                && !string.IsNullOrWhiteSpace(data.SixthSenseSubject))
            {
                character.HeroicConfiguration = new CharacterHeroicConfiguration
                {
                    Id = Guid.NewGuid(),
                    CharacterId = characterId,
                    SixthSenseSubject = data.SixthSenseSubject.Trim()[..Math.Min(
                        data.SixthSenseSubject.Trim().Length, HeroicParameterRules.SixthSenseSubjectMaxLength)],
                };
            }
            else if (kind == HeroicParameterKind.SignatureWeapon && data.SignatureWeaponProfile is { } profile)
            {
                try
                {
                    character.SignatureWeapon = new CharacterSignatureWeapon
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = characterId,
                        Profile = profile,
                        Craftsmanship = data.SignatureWeaponCraftsmanship ?? WeaponCraftsmanship.Steel,
                        NarrativeForm = HeroicParameterRules.ValidateNarrativeForm(data.SignatureWeaponForm),
                        FormTraits = HeroicParameterRules.ValidateFormTraits(
                            profile, data.SignatureWeaponTraits ?? WeaponFormTraits.None),
                        IsLost = data.SignatureWeaponLost,
                    };
                }
                catch (DomainRuleException ex)
                {
                    warnings.Add($"Именное оружие не перенесено: {ex.Message}");
                }
            }
        }
        if (character.HeroicConfigurationIncomplete)
        {
            warnings.Add(
                "Параметр героической способности не выбран — улучшения останутся заблокированы, "
                + "пока владелец не выберет его вручную.");
        }

        if (character.HeroicIdentityIncomplete)
        {
            warnings.Add(
                "Личное название и происхождение героической способности не заполнены — "
                + "улучшения останутся заблокированы, пока владелец не заполнит их вручную.");
        }

        ApplyThresholdSnapshot(character, archetype, data, warnings);

        var notes = (data.Notes ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n.Title) || !string.IsNullOrWhiteSpace(n.Body))
            .Select(n => new CharacterNote
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                OwnerUserId = userId,
                Title = string.IsNullOrWhiteSpace(n.Title) ? "Без названия" : n.Title.Trim(),
                Body = n.Body ?? "",
            })
            .ToList();

        return new ImportResolution(character, notes, Label(archetype.NameRu, archetype.Name), Label(career.NameRu, career.Name), warnings);
    }

    /// <summary>
    /// Переносит legacy-поле <c>GrantedCharacteristics</c> в общий формат выборов (ROT-TAL-03).
    /// Возвращает число созданных выборов; 0 — переносить было нечего.
    /// </summary>
    private static int LegacyGrantsToChoices(CharacterTalent talent, TalentChoiceSchema schema)
    {
        if (schema.Kind != TalentChoiceKind.Characteristic) return 0;

        var grants = talent.ParseGrants();
        for (var rank = 0; rank < grants.Count; rank++)
        {
            talent.Choices.Add(new CharacterTalentChoice
            {
                Id = Guid.NewGuid(),
                CharacterTalentId = talent.Id,
                RankIndex = rank,
                Kind = TalentChoiceKind.Characteristic,
                Value = grants[rank].ToString(),
                DisplayName = grants[rank].ToString(),
            });
        }
        return grants.Count;
    }

    /// <summary>
    /// Восстанавливает пороги ран/стрейна (ROT-CRE-02). Персонаж в фазе создания порогов не хранит.
    /// Файл v2 приносит их как есть. Файл v1 (или v2 без значений) — детерминированно считается
    /// «база вида + импортированная характеристика», помечается <c>LegacyEstimated</c> и требует
    /// ручной проверки: угадывать характеристику до Dedication нельзя, а ноль записывать запрещено.
    /// </summary>
    private static void ApplyThresholdSnapshot(
        Character character, ArchetypeDef archetype, CharacterExportData data, List<string> warnings)
    {
        if (character.IsCreationPhase)
        {
            character.CreationWoundThreshold = null;
            character.CreationStrainThreshold = null;
            character.ThresholdSnapshotProvenance = ThresholdSnapshotProvenance.None;
            return;
        }

        if (data.CreationWoundThreshold is { } wt and > 0 && data.CreationStrainThreshold is { } st and > 0)
        {
            character.CreationWoundThreshold = wt;
            character.CreationStrainThreshold = st;
            character.ThresholdSnapshotProvenance = data.ThresholdSnapshotProvenance == ThresholdSnapshotProvenance.None
                ? ThresholdSnapshotProvenance.CreationCompleted
                : data.ThresholdSnapshotProvenance;
            character.RulesReviewRequired = data.RulesReviewRequired;
            return;
        }

        character.CreationWoundThreshold = Math.Max(1, GenesysRules.WoundThreshold(archetype.WoundBase, character.Brawn));
        character.CreationStrainThreshold = Math.Max(1, GenesysRules.StrainThreshold(archetype.StrainBase, character.Willpower));
        character.ThresholdSnapshotProvenance = ThresholdSnapshotProvenance.LegacyEstimated;
        character.RulesReviewRequired = true;
        warnings.Add(
            "В файле нет зафиксированных порогов ран/стрейна. Они рассчитаны по текущим характеристикам "
            + "и помечены как требующие проверки: если персонаж повышал Мощь или Волю после создания, "
            + "пороги нужно исправить вручную.");
    }

    private static async Task<ArchetypeDef?> ResolveArchetypeAsync(
        IAppDbContext db, GameSystem system, string? code, string? name, CancellationToken ct)
    {
        ArchetypeDef? def = null;
        if (!string.IsNullOrWhiteSpace(code))
            def = await db.ArchetypeDefs.FirstOrDefaultAsync(a => a.System == system && a.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.ArchetypeDefs.FirstOrDefaultAsync(a => a.System == system && a.Name == name, ct);
        return def;
    }

    private static async Task<CareerDef?> ResolveCareerAsync(
        IAppDbContext db, GameSystem system, string? code, string? name, CancellationToken ct)
    {
        CareerDef? def = null;
        if (!string.IsNullOrWhiteSpace(code))
            def = await db.CareerDefs.FirstOrDefaultAsync(c => c.System == system && c.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.CareerDefs.FirstOrDefaultAsync(c => c.System == system && c.Name == name, ct);
        return def;
    }

    private static async Task<SkillDef?> ResolveSkillAsync(
        IAppDbContext db, Guid userId, GameSystem system, string? code, string? name, CancellationToken ct)
    {
        SkillDef? def = null;
        if (!string.IsNullOrWhiteSpace(code))
            def = await db.SkillDefs.FirstOrDefaultAsync(s => s.System == system && s.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.SkillDefs.FirstOrDefaultAsync(
                s => s.System == system && s.Name == name && (s.OwnerUserId == null || s.OwnerUserId == userId), ct);
        return def;
    }

    private static async Task<TalentDef?> ResolveTalentAsync(
        IAppDbContext db, Guid userId, GameSystem system, string? code, string? name, CancellationToken ct)
    {
        TalentDef? def = null;
        if (!string.IsNullOrWhiteSpace(code))
            def = await db.TalentDefs.FirstOrDefaultAsync(t => t.System == system && t.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.TalentDefs.FirstOrDefaultAsync(
                t => t.System == system && t.Name == name && (t.OwnerUserId == null || t.OwnerUserId == userId), ct);
        return def;
    }

    private static async Task<ItemDef?> ResolveItemAsync(
        IAppDbContext db, Guid userId, GameSystem system, string? code, string? name, CancellationToken ct)
    {
        ItemDef? def = null;
        if (!string.IsNullOrWhiteSpace(code))
            def = await db.ItemDefs.FirstOrDefaultAsync(i => i.System == system && i.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.ItemDefs.FirstOrDefaultAsync(
                i => i.System == system && i.Name == name && (i.OwnerUserId == null || i.OwnerUserId == userId), ct);
        return def;
    }

    private static async Task<HeroicAbilityDef?> ResolveHeroicAsync(
        IAppDbContext db, Guid userId, string? code, string? name, CancellationToken ct)
    {
        // У HeroicAbilityDef нет System — матчим по Code, затем по Name в области видимости владельца.
        HeroicAbilityDef? def = null;
        if (!string.IsNullOrWhiteSpace(code))
            def = await db.HeroicAbilityDefs.Include(h => h.Upgrades).FirstOrDefaultAsync(h => h.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.HeroicAbilityDefs.Include(h => h.Upgrades).FirstOrDefaultAsync(
                h => h.Name == name && (h.OwnerUserId == null || h.OwnerUserId == userId), ct);
        return def;
    }

    private static int Char(CharacterExportData d, string key, int fallback)
    {
        if (d.Characteristics is null) return fallback;
        foreach (var kv in d.Characteristics)
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return fallback;
    }

    private static string Display(string? name, string? code) =>
        !string.IsNullOrWhiteSpace(name) ? name! : !string.IsNullOrWhiteSpace(code) ? code! : "—";

    private static string Label(string ru, string en) => string.IsNullOrWhiteSpace(ru) ? en : ru;
}
