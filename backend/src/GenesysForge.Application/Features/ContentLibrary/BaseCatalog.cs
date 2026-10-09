using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentLibrary;

public static class BaseCatalog
{
    public static BaseContentCategory? Category(CustomEntryType type) => type switch
    {
        CustomEntryType.Skill => BaseContentCategory.Skill,
        CustomEntryType.Talent => BaseContentCategory.Talent,
        CustomEntryType.Item => BaseContentCategory.Item,
        CustomEntryType.Archetype => BaseContentCategory.Archetype,
        CustomEntryType.Career => BaseContentCategory.Career,
        CustomEntryType.HeroicAbility => BaseContentCategory.HeroicAbility,
        _ => null,
    };

    public static async Task<List<BaseCatalogEntryDto>> LoadAsync(IAppDbContext db, GameSystem system, CancellationToken ct)
    {
        if (!Enum.IsDefined(system)) throw new DomainRuleException("Неизвестная игровая система.");
        return (await LoadSnapshotAsync(db, ct)).ForSystem(system);
    }

    // Request-local snapshot, shared explicitly by composing handlers. Never caches user state globally.
    internal static async Task<BaseCatalogSnapshot> LoadSnapshotAsync(IAppDbContext db, CancellationToken ct)
    {
        var definitions = (await ContentDefinitions.LoadAsync(db, ct: ct, builtinOnly: true))
            .Where(x => !x.Retired && Category(x.Type) != null).ToList();
        static string BareCode(string code) => code.StartsWith("gc.") || code.StartsWith("rot.") ? code[(code.IndexOf('.') + 1)..] : code;
        var core = definitions.Where(x => x.System == GameSystem.GenesysCore)
            .Select(x => (x.Type, Code: BareCode(x.Code))).ToHashSet();
        var spells = await db.SpellDefs.AsNoTracking().Where(x => x.OwnerUserId == null && x.Kind == SpellEntryKind.Effect).ToListAsync(ct);
        var coreSpells = spells.Where(x => x.System == GameSystem.GenesysCore).Select(x => (x.MagicSkill, x.NameEn)).ToHashSet();
        var catalogs = new Dictionary<GameSystem, List<BaseCatalogEntryDto>>();
        foreach (var system in Enum.GetValues<GameSystem>())
        {
            var rows = definitions.Where(x => x.System == system)
                .Select(x => new BaseCatalogEntryDto(Category(x.Type)!.Value, x.Code, x.Name, x.NameRu, x.Meta,
                    system == GameSystem.RealmsOfTerrinoth && core.Contains((x.Type, BareCode(x.Code))))).ToList();
            rows.AddRange(spells.Where(x => x.System == system).Select(x => new BaseCatalogEntryDto(BaseContentCategory.Magic,
                CampaignContentPolicy.SpellKey(x), x.NameEn, x.NameRu, $"{x.MagicSkill} · {x.Difficulty}",
                system == GameSystem.RealmsOfTerrinoth && coreSpells.Contains((x.MagicSkill, x.NameEn)))));
            catalogs[system] = rows.OrderBy(x => x.Category).ThenBy(x => x.NameRu).ThenBy(x => x.Name).ToList();
        }
        return new(catalogs, definitions.ToDictionary(x => (x.System, Category(x.Type)!.Value, x.Code), x => x.Id));
    }
}

internal sealed class BaseCatalogSnapshot(
    Dictionary<GameSystem, List<BaseCatalogEntryDto>> catalogs,
    Dictionary<(GameSystem, BaseContentCategory, string), Guid> definitionIds)
{
    public Dictionary<(GameSystem, BaseContentCategory, string), Guid> DefinitionIds { get; } = definitionIds;
    public Dictionary<(GameSystem, BaseContentCategory, string), BaseCatalogEntryDto> ByKey { get; } =
        catalogs.SelectMany(c => c.Value.Select(x => (Key: (c.Key, x.Category, x.Key), Value: x))).ToDictionary(x => x.Key, x => x.Value);

    public List<BaseCatalogEntryDto> ForSystem(GameSystem system) => catalogs.TryGetValue(system, out var rows)
        ? rows : throw new DomainRuleException("Неизвестная игровая система.");
}
