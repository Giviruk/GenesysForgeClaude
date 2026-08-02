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
        var definitions = await ImportDefinitionSet.LoadAsync(db, userId, system, data, ct);

        var archetype = definitions.Archetypes.Resolve(data.ArchetypeCode, data.ArchetypeName)
            ?? throw new DomainRuleException(
                $"Не найден архетип «{Display(data.ArchetypeName, data.ArchetypeCode)}» для системы {system}.");
        var career = definitions.Careers.Resolve(data.CareerCode, data.CareerName)
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
            var def = definitions.Skills.Resolve(s.Code, s.Name);
            if (def is null) { warnings.Add($"Навык «{Display(s.Name, s.Code)}» не найден — пропущен."); continue; }
            character.Skills.Add(new CharacterSkill
            {
                Id = Guid.NewGuid(), CharacterId = characterId, SkillDefId = def.Id,
                Ranks = Math.Max(0, s.Ranks), IsCareer = s.IsCareer, FreeRanks = Math.Max(0, s.FreeRanks),
            });
        }

        foreach (var t in data.Talents ?? [])
        {
            var def = definitions.Talents.Resolve(t.Code, t.Name);
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

        // Пары «строка инвентаря → запись каталога»: навигация ItemDef у новых строк не заполняется,
        // а признаки формы нужны для проверки рук и брони ниже.
        var resolvedItems = new List<(CharacterItem Item, ItemDef Def)>();
        // Груз ссылается на транспорт индексом, а транспорт создаётся ниже: ссылки собираются здесь
        // и проставляются одним проходом после (ROT-TRANSPORT-01).
        var cargoLinks = new List<(CharacterItem Item, ItemDef Def, int MountIndex, bool Installed)>();
        foreach (var it in data.Items ?? [])
        {
            var def = definitions.Items.Resolve(it.Code, it.Name);
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
            // Состояние повреждения переносится как есть; неизвестное значение файла — целый
            // предмет, тем же правилом, что и у файлов прежних версий (GEN-EQP-DMG-01).
            var damageState = Enum.IsDefined(it.DamageState) ? it.DamageState : ItemDamageState.Undamaged;
            if (!Enum.IsDefined(it.DamageState))
                warnings.Add($"У предмета «{def.Name}» указано неизвестное состояние — оставлен целым.");
            var shardSpec = RuneboundShardRules.For(def.Code);
            var validShardConfiguration = false;
            if (shardSpec is { NeedsConfiguration: true } && it.ShardConfigured)
            {
                var activation = it.ShardActivationChoice?.Trim() ?? "";
                var configuredEffect = definitions.ConfiguredSpellEffect(
                    it.ShardEffectAction, it.ShardEffectChoice);
                validShardConfiguration = activation.Length is >= 3 and <= 500
                    && configuredEffect?.DifficultyIncrease == 1
                    && MagicMatrix.SkillsForEffect(
                            it.ShardEffectAction ?? "", it.ShardEffectChoice ?? "")
                        .Contains(RuneboundShardRules.RequiredMagicSkill, StringComparer.Ordinal);
                if (!validShardConfiguration)
                    warnings.Add(
                        $"Настройка Lesser Rune «{def.Name}» не прошла проверку и сброшена; "
                        + "ведущий должен настроить её заново.");
            }
            var item = new CharacterItem
            {
                Id = Guid.NewGuid(), CharacterId = characterId, ItemDefId = def.Id,
                Quantity = shardSpec is null ? Math.Max(1, it.Quantity) : 1, State = it.State,
                Craftsmanship = CraftsmanshipRules.FixedFor(def.Code) ?? craftsmanship,
                DamageState = damageState,
                // Материал переносится только тому, у кого он бывает: файл с «ивовым мешком»
                // чинится дубом, а не создаёт предмет с чужим свойством (ROT-MAG-MAT-01).
                ImplementMaterial = Enum.IsDefined(it.Material) && ImplementRules.IsImplement(def.Code)
                    ? it.Material
                    : ImplementMaterial.Oak,
                ImplementChoices = ImplementRules.IsImplement(def.Code) ? it.ImplementChoices : "",
                ImplementConfigured = ImplementRules.IsImplement(def.Code) && it.ImplementConfigured,
                ShardActivationChoice = validShardConfiguration
                    ? (it.ShardActivationChoice ?? "").Trim()
                    : "",
                ShardEffectAction = validShardConfiguration
                    ? it.ShardEffectAction ?? ""
                    : "",
                ShardEffectChoice = validShardConfiguration
                    ? it.ShardEffectChoice ?? ""
                    : "",
                ShardConfigured = validShardConfiguration,
                // Комплект и стартовый бюджет сохраняются как провенанс; всё остальное — Imported,
                // чтобы импорт не выглядел покупкой в истории нового персонажа.
                Provenance = it.Provenance is ItemProvenance.CareerPackage or ItemProvenance.StartingBudget
                    ? it.Provenance
                    : ItemProvenance.Imported,
            };
            character.Items.Add(item);
            resolvedItems.Add((item, def));
            if (it.CarriedByMountIndex is { } cargoMountIndex)
                cargoLinks.Add((item, def, cargoMountIndex, it.IsInstalledOnMount));
            // Старые экспорты могли хранить несколько shard одной строкой. Каждый shard является
            // отдельным implement instance, поэтому сохраняем количество отдельными строками.
            for (var copy = 1; shardSpec is not null && copy < Math.Max(1, it.Quantity); copy++)
            {
                var clone = new CharacterItem
                {
                    Id = Guid.NewGuid(), CharacterId = characterId, ItemDefId = def.Id,
                    Quantity = 1, State = item.State, Craftsmanship = item.Craftsmanship,
                    DamageState = item.DamageState, ImplementMaterial = item.ImplementMaterial,
                    ImplementChoices = item.ImplementChoices,
                    ImplementConfigured = item.ImplementConfigured,
                    ShardActivationChoice = item.ShardActivationChoice,
                    ShardEffectAction = item.ShardEffectAction ?? "",
                    ShardEffectChoice = item.ShardEffectChoice ?? "",
                    ShardConfigured = item.ShardConfigured,
                    Provenance = item.Provenance,
                };
                character.Items.Add(clone);
                resolvedItems.Add((clone, def));
            }
        }

        // Транспорт (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01; файлы v4 и v5). Профиль ищется по
        // стабильному коду с fallback на имя; ненайденный не выдумывается, а называется в
        // предупреждении. Раны приводятся к границам профиля — файл мог прийти с любым числом.
        // Порядок списка сохраняется: по нему груз и тяга ссылаются на транспорт индексами.
        var importedMounts = new List<(CharacterMount? Mount, MountDef? Def)>();
        foreach (var m in data.Mounts ?? [])
        {
            var def = definitions.Mounts.Resolve(m.Code, m.Name);
            if (def is null)
            {
                warnings.Add($"Транспорт «{Display(m.Name, m.Code)}» не найден — пропущен.");
                // Пропущенный транспорт остаётся дыркой в нумерации, иначе груз и тяга поехали бы
                // не на тот экземпляр.
                importedMounts.Add((null, null));
                continue;
            }

            // Груз файлов v4 был одним числом без позиций; переносить его нечем, и молча делать вид,
            // что его не было, нельзя.
            if (m.CarriedLoad > 0)
                warnings.Add(
                    $"У транспорта «{def.Name}» в файле указан груз {m.CarriedLoad} без описи — "
                    + "он не перенесён: груз теперь хранится позициями.");

            var mount = new CharacterMount
            {
                Id = Guid.NewGuid(),
                MountDefId = def.Id,
                Name = (m.CustomName ?? "").Trim(),
                WoundsCurrent = MountRules.ClampWounds(def, m.WoundsCurrent),
                IsActive = m.IsActive,
                Notes = m.Notes ?? "",
                Provenance = ItemProvenance.Imported,
            };
            character.Mounts.Add(mount);
            importedMounts.Add((mount, def));
        }

        LinkImportedTransport(data, importedMounts, cargoLinks, warnings);

        // Файл мог быть собран до правил о руках и броне: лишнее не выбрасывается, а перестаёт
        // считаться используемым — с предупреждением, чтобы владелец сам решил, что взять (ROT-EQP-01).
        var kept = new List<EquippedItemInput>();
        foreach (var (item, def) in resolvedItems.Where(x => x.Item.State == ItemState.Equipped))
        {
            var candidate = new EquippedItemInput(
                item.Id, def.Kind, def.FormTraits, def.Name,
                ImplementRules.IsImplement(def.Code) || RuneboundShardRules.IsShard(def.Code));
            if (EquipmentSlotRules.IsValid([.. kept, candidate])) { kept.Add(candidate); continue; }
            item.State = ItemState.Carried;
            warnings.Add(
                $"«{def.Name}» больше не используется: одновременно носят одну броню, держат две руки "
                + "и пользуются одним магическим инструментом.");
        }
        // Активной остаётся надетая броня — она теперь единственная.
        character.ActiveArmorCharacterItemId = resolvedItems
            .FirstOrDefault(x => x.Item.State == ItemState.Equipped && x.Def.Kind == ItemKind.Armor).Item?.Id;

        if (!string.IsNullOrWhiteSpace(data.HeroicAbilityCode) || !string.IsNullOrWhiteSpace(data.HeroicAbilityName))
        {
            var heroic = definitions.Heroics.Resolve(data.HeroicAbilityCode, data.HeroicAbilityName);
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
                var skill = definitions.Skills.Resolve(data.ParagonSkillCode, data.ParagonSkillName);
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
                    var craftsmanship = data.SignatureWeaponCraftsmanship ?? WeaponCraftsmanship.Steel;
                    var traits = HeroicParameterRules.ValidateFormTraits(
                        profile, data.SignatureWeaponTraits ?? WeaponFormTraits.None);
                    // Базовое улучшение переносится по коду и заново проверяется на совместимость:
                    // подменённый в файле код не должен стать оружием, которого правило не даёт.
                    // Файл до v6 его не содержит — параметр останется незавершённым.
                    Guid? baseAttachmentId = null;
                    if (!string.IsNullOrWhiteSpace(data.SignatureWeaponBaseAttachmentCode))
                    {
                        var code = data.SignatureWeaponBaseAttachmentCode.Trim();
                        var att = await db.AttachmentDefs.Include(a => a.Effects).FirstOrDefaultAsync(a =>
                            a.Code == code && a.System == system && !a.Retired && a.OwnerUserId == null, ct);
                        if (att is null)
                        {
                            warnings.Add($"Базовое улучшение именного оружия «{code}» не найдено — выберите его заново.");
                        }
                        else
                        {
                            try
                            {
                                HeroicParameterRules.EnsureCanBeBaseAttachment(
                                    traits,
                                    [.. SignatureWeaponProfiles.QualitiesFor(profile, craftsmanship).Select(q => q.Code)],
                                    att);
                                baseAttachmentId = att.Id;
                            }
                            catch (DomainRuleException ex)
                            {
                                warnings.Add($"Базовое улучшение именного оружия не перенесено: {ex.Message}");
                            }
                        }
                    }

                    character.SignatureWeapon = new CharacterSignatureWeapon
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = characterId,
                        Profile = profile,
                        Craftsmanship = craftsmanship,
                        NarrativeForm = HeroicParameterRules.ValidateNarrativeForm(data.SignatureWeaponForm),
                        FormTraits = traits,
                        BaseAttachmentDefId = baseAttachmentId,
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

    /// <summary>
    /// Расставляет ссылки груза и тяги после того, как транспорт создан (ROT-TRANSPORT-01). Битая
    /// ссылка не роняет импорт и не создаёт груз без владельца: позиция остаётся у персонажа, а в
    /// предупреждениях написано, что именно не приехало.
    /// </summary>
    private static void LinkImportedTransport(
        CharacterExportData data,
        List<(CharacterMount? Mount, MountDef? Def)> mounts,
        List<(CharacterItem Item, ItemDef Def, int MountIndex, bool Installed)> cargoLinks,
        List<string> warnings)
    {
        foreach (var (item, def, index, installed) in cargoLinks)
        {
            if (index < 0 || index >= mounts.Count || mounts[index].Mount is not { } mount)
            {
                warnings.Add(
                    $"Груз «{def.Name}» ссылается на транспорт, которого нет в файле — "
                    + "позиция оставлена у персонажа.");
                continue;
            }

            item.CarriedByMountId = mount.Id;
            item.CarriedByMount = mount;
            // Устанавливается только то, что вообще устанавливается: файл с «установленным мечом»
            // чинится обычным грузом, а не создаёт предмет с чужим свойством.
            item.IsInstalledOnMount = installed && ShopCatalogRules.IsMountGear(def.Code);
            if (installed && !item.IsInstalledOnMount)
                warnings.Add(
                    $"«{def.Name}» в файле помечен установленным на транспорт, но таким снаряжением "
                    + "не является — перенесён обычным грузом.");
        }

        var exports = data.Mounts ?? [];
        for (var i = 0; i < exports.Count && i < mounts.Count; i++)
        {
            if (exports[i].DrawnByMountIndex is not { } drawnIndex) continue;
            if (mounts[i] is not ({ } mount, { } def)) continue;

            if (drawnIndex == i || drawnIndex < 0 || drawnIndex >= mounts.Count
                || mounts[drawnIndex] is not ({ } draft, { } draftDef)
                || !MountRules.CanDraw(draftDef))
            {
                warnings.Add(
                    $"У транспорта «{def.Name}» тягловое животное из файла не подошло — "
                    + "связь не восстановлена.");
                continue;
            }

            mount.DrawnByMountId = draft.Id;
            mount.DrawnBy = draft;
        }

        // Перегруз файла сохраняется как есть — это состояние стола, а не ошибка импорта, — но
        // владелец должен о нём узнать.
        foreach (var (mount, def) in mounts)
        {
            if (mount is null || def is null) continue;
            var cargo = cargoLinks
                .Where(x => x.Item.CarriedByMountId == mount.Id)
                .Select(x => x.Item)
                .ToList();
            var load = MountRules.CargoLoad(cargo);
            var capacity = MountRules.Capacity(def, MountRules.InstalledCapacityBonus(cargo));
            if (load > capacity)
                warnings.Add(
                    $"У транспорта «{def.Name}» груз {load} больше вместимости {capacity} — "
                    + "перегруз сохранён как есть.");
        }
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
