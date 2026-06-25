using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
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
        if (payload is null || payload.Format != CharacterExportDto.CurrentFormat)
            throw new DomainRuleException($"Неподдерживаемый формат файла. Ожидается «{CharacterExportDto.CurrentFormat}».");
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
            character.Talents.Add(new CharacterTalent
            {
                Id = Guid.NewGuid(), CharacterId = characterId, TalentDefId = def.Id,
                Ranks = Math.Max(1, t.Ranks), GrantedCharacteristics = t.GrantedCharacteristics ?? "",
            });
        }

        foreach (var it in data.Items ?? [])
        {
            var def = await ResolveItemAsync(db, userId, system, it.Code, it.Name, ct);
            if (def is null) { warnings.Add($"Предмет «{Display(it.Name, it.Code)}» не найден — пропущен."); continue; }
            character.Items.Add(new CharacterItem
            {
                Id = Guid.NewGuid(), CharacterId = characterId, ItemDefId = def.Id,
                Quantity = Math.Max(1, it.Quantity), State = it.State,
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
                character.HeroicUpgradeRank = Math.Clamp(data.HeroicUpgradeRank, 0, 2);
            }
        }

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
            def = await db.HeroicAbilityDefs.FirstOrDefaultAsync(h => h.Code == code, ct);
        if (def is null && !string.IsNullOrWhiteSpace(name))
            def = await db.HeroicAbilityDefs.FirstOrDefaultAsync(
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
