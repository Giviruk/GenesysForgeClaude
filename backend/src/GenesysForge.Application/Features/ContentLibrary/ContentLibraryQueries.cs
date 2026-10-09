using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Application.Features.HomebrewPacks;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentLibrary;

public record GetLibraryQuery(Guid UserId, GameSystem? System = null) : IQuery<List<LibraryEntryDto>>;
public record GetBaseCatalogQuery(Guid UserId, GameSystem System) : IQuery<List<BaseCatalogEntryDto>>;
public record GetPackExclusionsQuery(Guid UserId, Guid PackId) : IQuery<List<BaseCatalogEntryDto>>;
public record GetCampaignContentQuery(Guid UserId, Guid CampaignId) : IQuery<CampaignContentDto>;
public record GetCampaignBaseQuery(Guid UserId, Guid CampaignId, GameSystem System, BaseContentCategory? Category = null) : IQuery<List<CampaignBaseEntryDto>>;
public record GetCampaignItemsQuery(Guid UserId, Guid CampaignId) : IQuery<List<CampaignContentItemDto>>;
public record GetLibraryProposalsQuery(Guid UserId) : IQuery<List<LibraryProposalDto>>;

public class GetLibraryHandler(IAppDbContext db) : IQueryHandler<GetLibraryQuery, List<LibraryEntryDto>>
{
    public async Task<List<LibraryEntryDto>> Handle(GetLibraryQuery q, CancellationToken ct = default)
    {
        var definitions = await ContentDefinitions.LoadAsync(db, q.System, q.UserId, ct);
        var ids = definitions.Select(x => x.Id).ToList();
        var entries = await db.HomebrewPackEntries.AsNoTracking().Where(x => ids.Contains(x.EntryId)).ToListAsync(ct);
        var edited = await db.CustomContentChanges.AsNoTracking().Where(x => ids.Contains(x.DefinitionId))
            .GroupBy(x => x.DefinitionId).Select(g => new { Id = g.Key, At = g.Max(x => x.CreatedAt) }).ToDictionaryAsync(x => x.Id, x => x.At, ct);
        return definitions.Select(x => new LibraryEntryDto(x.Type, x.Id, x.System, x.Name, x.NameRu, x.Meta,
            edited.TryGetValue(x.Id, out var at) ? at : null,
            entries.Where(e => e.EntryType == x.Type && e.EntryId == x.Id).Select(e => e.HomebrewPackId).ToList())).ToList();
    }
}
public class GetBaseCatalogHandler(IAppDbContext db) : IQueryHandler<GetBaseCatalogQuery, List<BaseCatalogEntryDto>>
{
    public Task<List<BaseCatalogEntryDto>> Handle(GetBaseCatalogQuery q, CancellationToken ct = default) => BaseCatalog.LoadAsync(db, q.System, ct);
}
public class GetPackExclusionsHandler(IAppDbContext db) : IQueryHandler<GetPackExclusionsQuery, List<BaseCatalogEntryDto>>
{
    public async Task<List<BaseCatalogEntryDto>> Handle(GetPackExclusionsQuery q, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, q.PackId, ct);
        var keys = (await db.HomebrewPackExclusions.AsNoTracking().Where(x => x.HomebrewPackId == q.PackId).ToListAsync(ct)).Select(x => (x.Category, x.ContentKey)).ToHashSet();
        return (await BaseCatalog.LoadAsync(db, pack.System, ct)).Where(x => keys.Contains((x.Category, x.Key))).ToList();
    }
}
public class GetCampaignBaseHandler(IAppDbContext db) : IQueryHandler<GetCampaignBaseQuery, List<CampaignBaseEntryDto>>
{
    public async Task<List<CampaignBaseEntryDto>> Handle(GetCampaignBaseQuery q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        return await LoadAsync(q, await BaseCatalog.LoadSnapshotAsync(db, ct), true, ct);
    }

    internal async Task<List<CampaignBaseEntryDto>> LoadAsync(GetCampaignBaseQuery q, BaseCatalogSnapshot snapshot,
        bool includeUses, CancellationToken ct, List<ContentUse>? sharedUses = null)
    {
        var catalog = snapshot.ForSystem(q.System).Where(x => q.Category == null || x.Category == q.Category).ToList();
        var states = (await LoadStatesAsync(q, snapshot, ct)).ToDictionary(x => (x.Category, x.Key));
        var uses = includeUses ? sharedUses ?? await UsesAsync(q.CampaignId, ct) : [];
        var useIndex = uses.GroupBy(x => (x.Category, x.DefinitionId)).ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().Order().ToList());
        return catalog.Select(x =>
        {
            var state = states[(x.Category, x.Key)];
            var usedBy = snapshot.DefinitionIds.TryGetValue((q.System, x.Category, x.Key), out var id)
                ? useIndex.GetValueOrDefault((x.Category, id), []) : [];
            return new CampaignBaseEntryDto(x.Category, x.Key, x.Name, x.NameRu, x.Meta, x.IsSharedWithCore,
                state.Enabled, state.Source, state.SourcePackName, usedBy);
        }).ToList();
    }

    internal record BaseState(BaseContentCategory Category, string Key, bool Enabled, string? Source, string? SourcePackName);

    // Commands need only availability/keys, not display metadata or character usage DTOs.
    internal async Task<List<BaseState>> LoadStatesAsync(GetCampaignBaseQuery q, BaseCatalogSnapshot snapshot, CancellationToken ct)
    {
        var catalog = snapshot.ForSystem(q.System).Where(x => q.Category == null || x.Category == q.Category);
        var links = await (from l in db.HomebrewPackCampaigns.AsNoTracking()
            join p in db.HomebrewPacks.AsNoTracking() on l.HomebrewPackId equals p.Id
            where l.CampaignId == q.CampaignId && l.IsEnabled && l.Status == ContentConnectionStatus.Active && p.System == q.System
            orderby p.Name, p.Id select new { p.Id, p.Name }).ToListAsync(ct);
        var packIds = links.Select(x => x.Id).ToList();
        var exclusions = await db.HomebrewPackExclusions.AsNoTracking().Where(x => packIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        var overrides = await db.CampaignBaseOverrides.AsNoTracking().Where(x => x.CampaignId == q.CampaignId && x.System == q.System).ToListAsync(ct);
        var exclusionIndex = exclusions.GroupBy(x => (x.Category, x.ContentKey))
            .ToDictionary(g => g.Key, g => g.Select(x => x.HomebrewPackId).ToHashSet());
        var overrideIndex = overrides.ToDictionary(x => (x.Category, x.ContentKey));
        var linkOrder = links.Select((x, i) => (x.Id, x.Name, Order: i)).ToDictionary(x => x.Id);
        return catalog.Select(x =>
        {
            var excluded = exclusionIndex.GetValueOrDefault((x.Category, x.Key), []);
            var ov = overrideIndex.GetValueOrDefault((x.Category, x.Key));
            var enabled = ov?.IsEnabled ?? excluded.Count == 0;
            var source = ov is not null ? (ov.IsEnabled ? "restored" : "manual") : excluded.Count > 0 ? "pack" : null;
            var packName = excluded.Count == 0 ? null : excluded.Select(id => linkOrder[id]).MinBy(p => p.Order)!.Name;
            return new BaseState(x.Category, x.Key, enabled, source, packName);
        }).ToList();
    }

    internal record ContentUse(BaseContentCategory Category, Guid DefinitionId, string Name);
    internal async Task<List<ContentUse>> UsesAsync(Guid campaignId, CancellationToken ct)
    {
        var characters = from cc in db.CampaignCharacters.AsNoTracking()
            join c in db.Characters.AsNoTracking() on cc.CharacterId equals c.Id
            where cc.CampaignId == campaignId select c;
        // A bounded number of queries, independent of catalogue size. Materialize before
        // concatenating records so both PostgreSQL and the in-memory provider can translate.
        var uses = await characters.Select(c => new ContentUse(BaseContentCategory.Archetype, c.ArchetypeId, c.Name)).ToListAsync(ct);
        uses.AddRange(await characters.Select(c => new ContentUse(BaseContentCategory.Career, c.CareerId, c.Name)).ToListAsync(ct));
        uses.AddRange(await characters.Where(c => c.HeroicAbilityId != null).Select(c => new ContentUse(BaseContentCategory.HeroicAbility, c.HeroicAbilityId!.Value, c.Name)).ToListAsync(ct));
        uses.AddRange(await (from c in characters join s in db.CharacterSkills on c.Id equals s.CharacterId where s.Ranks > 0 select new ContentUse(BaseContentCategory.Skill, s.SkillDefId, c.Name)).ToListAsync(ct));
        uses.AddRange(await (from c in characters join t in db.CharacterTalents on c.Id equals t.CharacterId select new ContentUse(BaseContentCategory.Talent, t.TalentDefId, c.Name)).ToListAsync(ct));
        uses.AddRange(await (from c in characters join i in db.CharacterItems on c.Id equals i.CharacterId select new ContentUse(BaseContentCategory.Item, i.ItemDefId, c.Name)).ToListAsync(ct));
        return uses;
    }
}
public class GetCampaignItemsHandler(IAppDbContext db) : IQueryHandler<GetCampaignItemsQuery, List<CampaignContentItemDto>>
{
    public async Task<List<CampaignContentItemDto>> Handle(GetCampaignItemsQuery q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var rows = await db.CampaignContentItems.AsNoTracking().Where(x => x.CampaignId == q.CampaignId).ToListAsync(ct);
        var definitions = await ContentDefinitions.LoadAsync(db, ct: ct, ids: rows.Select(x => x.EntryId).ToList());
        var owners = definitions.Where(x => rows.Any(r => r.EntryType == x.Type && r.EntryId == x.Id)).Select(x => x.OwnerUserId).ToList();
        var names = await db.Users.Where(x => owners.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
        return rows.Select(r => (Row: r, Def: definitions.FirstOrDefault(x => x.Type == r.EntryType && x.Id == r.EntryId))).Where(x => x.Def != null)
            .Select(x => new CampaignContentItemDto(x.Row.Id, x.Row.EntryType, x.Row.EntryId, x.Def!.System, x.Def.Name, x.Def.NameRu,
                x.Def.Meta, x.Row.IsEnabled, x.Row.Status, names.GetValueOrDefault(x.Def.OwnerUserId ?? Guid.Empty, ""), x.Def.OwnerUserId == q.UserId, x.Row.ProposedByUserId)).ToList();
    }
}
public class GetCampaignContentHandler(IAppDbContext db) : IQueryHandler<GetCampaignContentQuery, CampaignContentDto>
{
    public async Task<CampaignContentDto> Handle(GetCampaignContentQuery q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var snapshot = await BaseCatalog.LoadSnapshotAsync(db, ct);
        var baseHandler = new GetCampaignBaseHandler(db);
        var uses = await baseHandler.UsesAsync(q.CampaignId, ct);
        List<CampaignSystemContentDto> systems = [];
        List<CampaignContentAlertDto> alerts = [];
        foreach (var system in Enum.GetValues<GameSystem>())
        {
            var rows = await baseHandler.LoadAsync(new(q.UserId, q.CampaignId, system), snapshot, true, ct, uses);
            var characters = await (from cc in db.CampaignCharacters join c in db.Characters on cc.CharacterId equals c.Id
                where cc.CampaignId == q.CampaignId && c.System == system select new PackCampaignDto(c.Id, c.Name)).ToListAsync(ct);
            var isOpen = !await db.CampaignSystemSettings.AnyAsync(x => x.CampaignId == q.CampaignId && x.System == system && !x.IsOpen, ct);
            systems.Add(new(system, isOpen, characters, rows.GroupBy(x => x.Category).Select(g => new CampaignContentCategoryDto(g.Key, g.Count(), g.Count(x => x.Enabled))).ToList()));
            var used = rows.Where(x => !x.Enabled && x.UsedBy.Count > 0).ToList();
            if (used.Count > 0) alerts.Add(new("disabledInUse", null, system, used.Count, used.SelectMany(x => x.UsedBy).Distinct().ToList(), used.Select(x => new BaseCatalogEntryDto(x.Category,x.Key,x.Name,x.NameRu,x.Meta,x.IsSharedWithCore)).ToList()));
        }
        var packs = await new GetCampaignHomebrewPacksHandler(db).LoadAsync(new(q.UserId, q.CampaignId), snapshot, ct);
        var items = await new GetCampaignItemsHandler(db).Handle(new(q.UserId, q.CampaignId), ct);
        alerts.AddRange(packs.Where(x => x.Status == ContentConnectionStatus.Pending).Select(x => new CampaignContentAlertDto("pendingPack", x.Id)));
        alerts.AddRange(packs.Where(x => x.ChangedAfterConnection).Select(x => new CampaignContentAlertDto("packChanged", x.Id)));
        alerts.AddRange(packs.Where(x => x.Status == ContentConnectionStatus.Active && x.Entries?.Any(e => e.State == "pending") == true)
            .Select(x => new CampaignContentAlertDto("pendingEntries", x.Id)));
        alerts.AddRange(items.Where(x => x.Status == ContentConnectionStatus.Pending).Select(x => new CampaignContentAlertDto("pendingItem", x.Id)));
        var baseCount = systems.Where(x => x.IsOpen).Sum(x => x.Categories.Sum(c => c.Enabled));
        // Deduplicate items appearing in multiple packs or also attached directly.
        var customCount = packs.Where(x => x.IsEnabled && x.Status == ContentConnectionStatus.Active)
            .SelectMany(x => x.Entries ?? []).Where(x => x.State == "enabled").Select(x => (x.EntryType, x.EntryId))
            .Concat(items.Where(x => x.IsEnabled && x.Status == ContentConnectionStatus.Active).Select(x => (x.EntryType, x.EntryId))).Distinct().Count();
        return new(systems, packs, items, await db.CampaignBaseOverrides.CountAsync(x => x.CampaignId == q.CampaignId, ct), alerts, baseCount + customCount);
    }
}
public class GetLibraryProposalsHandler(IAppDbContext db) : IQueryHandler<GetLibraryProposalsQuery, List<LibraryProposalDto>>
{
    public async Task<List<LibraryProposalDto>> Handle(GetLibraryProposalsQuery q, CancellationToken ct = default)
    {
        var items = await (from i in db.CampaignContentItems.AsNoTracking() join c in db.Campaigns.AsNoTracking() on i.CampaignId equals c.Id
            where i.ProposedByUserId == q.UserId && db.CampaignMembers.Any(m => m.CampaignId == c.Id && m.UserId == q.UserId)
            select new LibraryProposalDto(c.Id, c.Name, "item", i.EntryId, i.Status)).ToListAsync(ct);
        items.AddRange(await (from p in db.HomebrewPackCampaigns.AsNoTracking() join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id
            where p.ProposedByUserId == q.UserId && db.CampaignMembers.Any(m => m.CampaignId == c.Id && m.UserId == q.UserId)
            select new LibraryProposalDto(c.Id, c.Name, "pack", p.HomebrewPackId, p.Status)).ToListAsync(ct));
        return items;
    }
}
