using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Common;
using GenesysForge.Application.Features.ContentLibrary;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.HomebrewPacks;

public record GetCampaignHomebrewPacksQuery(Guid UserId, Guid CampaignId) : IQuery<List<CampaignHomebrewPackDto>>;
public record ConnectSharedCampaignHomebrewPackCommand(Guid UserId, Guid CampaignId, string Token) : ICommand<HomebrewPackImportResult>;

public class GetCampaignHomebrewPacksHandler(IAppDbContext db)
    : IQueryHandler<GetCampaignHomebrewPacksQuery, List<CampaignHomebrewPackDto>>
{
    public async Task<List<CampaignHomebrewPackDto>> Handle(GetCampaignHomebrewPacksQuery query, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);
        var rows = await (from link in db.HomebrewPackCampaigns.AsNoTracking()
            join pack in db.HomebrewPacks.AsNoTracking() on link.HomebrewPackId equals pack.Id
            join owner in db.Users.AsNoTracking() on pack.OwnerUserId equals owner.Id
            where link.CampaignId == query.CampaignId
            orderby pack.Name
            select new { Pack = pack, LinkId = link.Id, link.IsEnabled, link.Status, link.UpdatePolicy, link.ProposedByUserId, OwnerName = owner.DisplayName, ConnectedAt = link.UpdatedAt,
                OwnerIsMember = pack.OwnerUserId == campaign.GmUserId || db.CampaignMembers.Any(
                    m => m.CampaignId == query.CampaignId && m.UserId == pack.OwnerUserId) }).ToListAsync(ct);
        var counts = await HomebrewPackMapper.CountEntriesAsync(db, rows.Select(r => r.Pack.Id).ToHashSet(), ct);
        var packIds = rows.Select(r => r.Pack.Id).ToHashSet();
        var entries = await db.HomebrewPackEntries.AsNoTracking().Where(x => packIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        var definitions = await ContentDefinitions.LoadAsync(db, ct: ct, ids:entries.Select(x => x.EntryId).ToList());
        var linkIds = rows.Select(x => x.LinkId).ToList();
        var states = await db.CampaignPackEntryStates.AsNoTracking().Where(x => linkIds.Contains(x.HomebrewPackCampaignId)).ToListAsync(ct);
        var exclusions = await db.HomebrewPackExclusions.AsNoTracking().Where(x => packIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        var catalogs = new Dictionary<GameSystem, List<BaseCatalogEntryDto>>();
        foreach (var system in rows.Select(x => x.Pack.System).Distinct()) catalogs[system] = await BaseCatalog.LoadAsync(db, system, ct);
        var changed = await db.CustomContentChanges.AsNoTracking()
            .Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value))
            .GroupBy(x => x.HomebrewPackId).Select(g => new { Id = g.Key!.Value, At = g.Max(x => x.CreatedAt) })
            .ToDictionaryAsync(x => x.Id, x => x.At, ct);
        return rows.Select(r =>
        {
            DateTime? lastChangedAt = changed.TryGetValue(r.Pack.Id, out var at) ? at : null;
            var changedAfterConnection = r.IsEnabled && r.Status == ContentConnectionStatus.Active && r.Pack.OwnerUserId != campaign.GmUserId
                && lastChangedAt.HasValue && lastChangedAt.Value > r.ConnectedAt;
            return new CampaignHomebrewPackDto(r.Pack.Id, r.Pack.Name, r.Pack.System,
                r.IsEnabled, r.Pack.OwnerUserId == query.UserId, counts.GetValueOrDefault(r.Pack.Id), r.OwnerName, r.OwnerIsMember,
                lastChangedAt, r.ConnectedAt, changedAfterConnection, r.UpdatePolicy, r.Status,
                exclusions.Count(x => x.HomebrewPackId == r.Pack.Id),
                entries.Where(x => x.HomebrewPackId == r.Pack.Id).Select(x =>
                {
                    var def = definitions.FirstOrDefault(d => d.Id == x.EntryId && d.Type == x.EntryType);
                    var state = states.FirstOrDefault(st => st.HomebrewPackCampaignId == r.LinkId && st.EntryType == x.EntryType && st.EntryId == x.EntryId);
                    return new CampaignPackEntryDto(x.EntryType, x.EntryId, def?.Name ?? "", def?.NameRu ?? "", state == null ? "enabled" : state.State == PackEntryState.Pending ? "pending" : "disabled");
                }).ToList(), r.ProposedByUserId,
                catalogs[r.Pack.System].Where(x => exclusions.Any(e => e.HomebrewPackId == r.Pack.Id && e.Category == x.Category && e.ContentKey == x.Key)).ToList());
        }).ToList();
    }
}

public class ConnectSharedCampaignHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<ConnectSharedCampaignHomebrewPackCommand, HomebrewPackImportResult>
{
    public async Task<HomebrewPackImportResult> Handle(ConnectSharedCampaignHomebrewPackCommand command, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var hash = HomebrewPackTokens.Hash(command.Token);
        var pack = await db.HomebrewPacks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsShared && p.ShareTokenHash == hash, ct)
            ?? throw new DomainRuleException("Shared-набор не найден.", "homebrew.shared_not_found");
        if (pack.OwnerUserId != campaign.GmUserId && !await db.CampaignMembers.AnyAsync(
                m => m.CampaignId == campaign.Id && m.UserId == pack.OwnerUserId, ct))
            throw new DomainRuleException("Подключить можно только набор участника кампании.", "homebrew.owner_not_member");
        var row = await db.HomebrewPackCampaigns.FirstOrDefaultAsync(
            x => x.HomebrewPackId == pack.Id && x.CampaignId == command.CampaignId, ct);
        if (row is null)
            db.HomebrewPackCampaigns.Add(new HomebrewPackCampaign
            {
                Id = Guid.NewGuid(), HomebrewPackId = pack.Id, CampaignId = command.CampaignId, IsEnabled = true,
            });
        else
        {
            row.IsEnabled = true;
            row.Status = ContentConnectionStatus.Active;
        }
        await db.SaveChangesAsync(ct);
        var counts = await HomebrewPackMapper.CountEntriesAsync(db, [pack.Id], ct);
        return new HomebrewPackImportResult(pack.Id, pack.Name, counts.GetValueOrDefault(pack.Id));
    }
}
