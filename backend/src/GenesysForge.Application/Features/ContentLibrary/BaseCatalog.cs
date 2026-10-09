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
        var definitions = await ContentDefinitions.LoadAsync(db, ct: ct, builtinOnly:true);
        var core = definitions.Where(x => x.System == GameSystem.GenesysCore && x.OwnerUserId == null && !x.Retired).ToList();
        static string BareCode(string code) => code.StartsWith("gc.") || code.StartsWith("rot.") ? code[(code.IndexOf('.') + 1)..] : code;
        var result = definitions.Where(x => x.System == system && x.OwnerUserId == null && !x.Retired && Category(x.Type) != null)
            .Select(x => new BaseCatalogEntryDto(Category(x.Type)!.Value, x.Code, x.Name, x.NameRu, x.Meta,
                system == GameSystem.RealmsOfTerrinoth && core.Any(c => c.Type == x.Type && BareCode(c.Code) == BareCode(x.Code))))
            .ToList();
        var spells = await db.SpellDefs.AsNoTracking().Where(x => x.System == system && x.OwnerUserId == null && x.Kind == SpellEntryKind.Effect).ToListAsync(ct);
        var coreSpells = await db.SpellDefs.AsNoTracking().Where(x => x.System == GameSystem.GenesysCore && x.OwnerUserId == null && x.Kind == SpellEntryKind.Effect).Select(x => new { x.MagicSkill, x.NameEn }).ToListAsync(ct);
        result.AddRange(spells.Select(x => new BaseCatalogEntryDto(BaseContentCategory.Magic, CampaignContentPolicy.SpellKey(x),
            x.NameEn, x.NameRu, $"{x.MagicSkill} · {x.Difficulty}", system == GameSystem.RealmsOfTerrinoth && coreSpells.Any(c => c.MagicSkill == x.MagicSkill && c.NameEn == x.NameEn))));
        return result.OrderBy(x => x.Category).ThenBy(x => x.NameRu).ThenBy(x => x.Name).ToList();
    }
}
