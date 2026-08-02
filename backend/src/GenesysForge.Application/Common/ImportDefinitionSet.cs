using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>
/// Один пакет справочников для импорта/предпросмотра персонажа. Раньше каждая строка файла
/// выполняла собственный запрос с fallback по имени; теперь число запросов постоянно и не зависит
/// от количества навыков, талантов, предметов и транспорта в файле.
/// </summary>
internal sealed class ImportDefinitionSet
{
    private ImportDefinitionSet(
        ImportDefinitionLookup<ArchetypeDef> archetypes,
        ImportDefinitionLookup<CareerDef> careers,
        ImportDefinitionLookup<SkillDef> skills,
        ImportDefinitionLookup<TalentDef> talents,
        ImportDefinitionLookup<ItemDef> items,
        ImportDefinitionLookup<MountDef> mounts,
        ImportDefinitionLookup<HeroicAbilityDef> heroics,
        IReadOnlyList<SpellDef> configuredSpellEffects)
    {
        Archetypes = archetypes;
        Careers = careers;
        Skills = skills;
        Talents = talents;
        Items = items;
        Mounts = mounts;
        Heroics = heroics;
        ConfiguredSpellEffects = configuredSpellEffects;
    }

    public ImportDefinitionLookup<ArchetypeDef> Archetypes { get; }
    public ImportDefinitionLookup<CareerDef> Careers { get; }
    public ImportDefinitionLookup<SkillDef> Skills { get; }
    public ImportDefinitionLookup<TalentDef> Talents { get; }
    public ImportDefinitionLookup<ItemDef> Items { get; }
    public ImportDefinitionLookup<MountDef> Mounts { get; }
    public ImportDefinitionLookup<HeroicAbilityDef> Heroics { get; }
    private IReadOnlyList<SpellDef> ConfiguredSpellEffects { get; }

    public SpellDef? ConfiguredSpellEffect(string? parentEffect, string? nameEn) =>
        ConfiguredSpellEffects.FirstOrDefault(s =>
            s.ParentEffect == parentEffect && s.NameEn == nameEn);

    public static async Task<ImportDefinitionSet> LoadAsync(
        IAppDbContext db, Guid userId, GameSystem system, CharacterExportData data, CancellationToken ct)
    {
        var archetypeCodes = Values(data.ArchetypeCode);
        var archetypeNames = Values(data.ArchetypeName);
        var careerCodes = Values(data.CareerCode);
        var careerNames = Values(data.CareerName);
        var skillCodes = Values((data.Skills ?? []).Select(x => x.Code).Append(data.ParagonSkillCode));
        var skillNames = Values((data.Skills ?? []).Select(x => x.Name).Append(data.ParagonSkillName));
        var talentCodes = Values((data.Talents ?? []).Select(x => x.Code));
        var talentNames = Values((data.Talents ?? []).Select(x => x.Name));
        var itemCodes = Values((data.Items ?? []).Select(x => x.Code));
        var itemNames = Values((data.Items ?? []).Select(x => x.Name));
        var mountCodes = Values((data.Mounts ?? []).Select(x => x.Code));
        var mountNames = Values((data.Mounts ?? []).Select(x => x.Name));
        var heroicCodes = Values(data.HeroicAbilityCode);
        var heroicNames = Values(data.HeroicAbilityName);
        var shardParents = Values((data.Items ?? []).Where(x => x.ShardConfigured).Select(x => x.ShardEffectAction));
        var shardEffects = Values((data.Items ?? []).Where(x => x.ShardConfigured).Select(x => x.ShardEffectChoice));

        // Загружаются только built-in записи и custom-контент импортирующего. Это одновременно
        // ускоряет импорт и закрывает возможность разрешить чужую запись по угаданному Code.
        // Фильтры Code/Name не вытаскивают весь каталог ради нескольких строк файла.
        var archetypes = await db.ArchetypeDefs.AsNoTracking()
            .Where(x => x.System == system && (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (archetypeCodes.Contains(x.Code) || archetypeNames.Contains(x.Name)))
            .ToListAsync(ct);
        var careers = await db.CareerDefs.AsNoTracking()
            .Where(x => x.System == system && (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (careerCodes.Contains(x.Code) || careerNames.Contains(x.Name)))
            .ToListAsync(ct);
        var skills = await db.SkillDefs.AsNoTracking()
            .Where(x => x.System == system && (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (skillCodes.Contains(x.Code) || skillNames.Contains(x.Name)))
            .ToListAsync(ct);
        var talents = await db.TalentDefs.AsNoTracking()
            .Where(x => x.System == system && (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (talentCodes.Contains(x.Code) || talentNames.Contains(x.Name)))
            .ToListAsync(ct);
        var items = await db.ItemDefs.AsNoTracking()
            .Where(x => x.System == system && (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (itemCodes.Contains(x.Code) || itemNames.Contains(x.Name)))
            .ToListAsync(ct);
        var mounts = await db.MountDefs.AsNoTracking()
            .Where(x => x.System == system && (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (mountCodes.Contains(x.Code) || mountNames.Contains(x.Name)))
            .ToListAsync(ct);
        var heroics = await db.HeroicAbilityDefs.Include(x => x.Upgrades)
            .Where(x => (x.OwnerUserId == null || x.OwnerUserId == userId)
                && (heroicCodes.Contains(x.Code) || heroicNames.Contains(x.Name)))
            .ToListAsync(ct);
        var configuredSpellEffects = await db.SpellDefs.AsNoTracking()
            .Where(x => x.System == system && x.Kind == SpellEntryKind.AdditionalEffect
                && shardParents.Contains(x.ParentEffect) && shardEffects.Contains(x.NameEn))
            .ToListAsync(ct);

        return new ImportDefinitionSet(
            Lookup(archetypes), Lookup(careers), Lookup(skills), Lookup(talents),
            Lookup(items), Lookup(mounts), Lookup(heroics), configuredSpellEffects);
    }

    private static HashSet<string> Values(params string?[] values) => Values(values.AsEnumerable());

    private static HashSet<string> Values(IEnumerable<string?> values) =>
        values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToHashSet(StringComparer.Ordinal);

    private static ImportDefinitionLookup<T> Lookup<T>(IEnumerable<T> definitions)
        where T : class, IContentDef => new(
            definitions,
            x => x.Code,
            x => x switch
            {
                ArchetypeDef value => value.Name,
                CareerDef value => value.Name,
                SkillDef value => value.Name,
                TalentDef value => value.Name,
                ItemDef value => value.Name,
                MountDef value => value.Name,
                HeroicAbilityDef value => value.Name,
                _ => "",
            },
            x => x switch
            {
                ArchetypeDef value => value.OwnerUserId,
                CareerDef value => value.OwnerUserId,
                SkillDef value => value.OwnerUserId,
                TalentDef value => value.OwnerUserId,
                ItemDef value => value.OwnerUserId,
                MountDef value => value.OwnerUserId,
                HeroicAbilityDef value => value.OwnerUserId,
                _ => null,
            });
}

/// <summary>
/// Видимый built-in/custom контент разрешается по Code, затем по имени. Built-in запись имеет
/// приоритет при совпадении ключа. Чужие записи в lookup не загружаются, поэтому custom Code не
/// может обойти owner visibility.
/// </summary>
internal sealed class ImportDefinitionLookup<T>
{
    private readonly Dictionary<string, T> _byVisibleCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, T> _byVisibleName = new(StringComparer.Ordinal);

    public ImportDefinitionLookup(
        IEnumerable<T> definitions,
        Func<T, string> code,
        Func<T, string> name,
        Func<T, Guid?> ownerUserId)
    {
        foreach (var definition in definitions.OrderBy(x => ownerUserId(x).HasValue))
        {
            var definitionName = name(definition);
            if (!string.IsNullOrWhiteSpace(definitionName))
                _byVisibleName.TryAdd(definitionName, definition);

            var definitionCode = code(definition);
            if (!string.IsNullOrWhiteSpace(definitionCode))
                _byVisibleCode.TryAdd(definitionCode, definition);
        }
    }

    public T? Resolve(string? code, string? name)
    {
        if (!string.IsNullOrWhiteSpace(code) && _byVisibleCode.TryGetValue(code, out var byCode))
            return byCode;
        if (!string.IsNullOrWhiteSpace(name) && _byVisibleName.TryGetValue(name, out var byName))
            return byName;
        return default;
    }
}
