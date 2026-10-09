using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Application.Features.HomebrewPacks;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentLibrary;

public record SetCampaignSystemCommand(Guid UserId, Guid CampaignId, GameSystem System, bool IsOpen) : ICommand<Unit>;
public record SetCampaignBaseCommand(Guid UserId, Guid CampaignId, BaseOverridesRequest Request) : ICommand<Unit>;
public record ResetCampaignBaseCommand(Guid UserId, Guid CampaignId, GameSystem System) : ICommand<Unit>;
public record SaveCampaignRestrictionsCommand(Guid UserId, Guid CampaignId, SaveRestrictionsRequest Request) : ICommand<HomebrewPackListItemDto>;
public record DisconnectCampaignPackCommand(Guid UserId, Guid CampaignId, Guid PackId) : ICommand<Unit>;
public record SetCampaignPackEntriesCommand(Guid UserId, Guid CampaignId, Guid PackId, CampaignPackEntriesRequest Request) : ICommand<Unit>;
public record ConnectCampaignItemsCommand(Guid UserId, Guid CampaignId, ContentEntriesRequest Request) : ICommand<Unit>;
public record SetCampaignItemCommand(Guid UserId, Guid CampaignId, Guid ItemId, bool IsEnabled) : ICommand<Unit>;
public record RemoveCampaignItemCommand(Guid UserId, Guid CampaignId, Guid ItemId) : ICommand<Unit>;
public record ProposeContentCommand(Guid UserId, Guid CampaignId, ContentProposalRequest Request) : ICommand<Unit>;
public record DecideContentCommand(Guid UserId, Guid CampaignId, string Kind, Guid TargetId, string Decision) : ICommand<Unit>;
public record WithdrawContentCommand(Guid UserId, Guid CampaignId, string Kind, Guid TargetId) : ICommand<Unit>;

public class SetCampaignSystemHandler(IAppDbContext db) : ICommandHandler<SetCampaignSystemCommand, Unit>
{
    public async Task<Unit> Handle(SetCampaignSystemCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        if (!Enum.IsDefined(q.System)) throw new DomainRuleException("Неизвестная игровая система.");
        var row = await db.CampaignSystemSettings.FindAsync([q.CampaignId, q.System], ct);
        if (row == null) db.CampaignSystemSettings.Add(new CampaignSystemSetting { CampaignId = q.CampaignId, System = q.System, IsOpen = q.IsOpen });
        else row.IsOpen = q.IsOpen;
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class SetCampaignBaseHandler(IAppDbContext db) : ICommandHandler<SetCampaignBaseCommand, Unit>
{
    public async Task<Unit> Handle(SetCampaignBaseCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var req = q.Request;
        var baseline = await new GetCampaignBaseHandler(db).Handle(new(q.UserId, q.CampaignId, req.System), ct);
        var requests = req.Items.GroupBy(x => (x.Category, x.Key)).Select(g => g.Last()).ToList();
        if (requests.Any(i => !baseline.Any(x => x.Category == i.Category && x.Key == i.Key)))
            throw new DomainRuleException("Неизвестный элемент встроенного каталога.");
        var rows = await db.CampaignBaseOverrides.Where(x => x.CampaignId == q.CampaignId && x.System == req.System).ToListAsync(ct);
        foreach (var item in requests)
        {
            var b = baseline.Single(x => x.Category == item.Category && x.Key == item.Key);
            // Independent of existing overrides: an active pack excludes iff it is named as source.
            var defaultEnabled = b.SourcePackName == null;
            var row = rows.FirstOrDefault(x => x.Category == item.Category && x.ContentKey == item.Key);
            if (item.Enabled == null || item.Enabled == defaultEnabled)
            { if (row != null) db.CampaignBaseOverrides.Remove(row); }
            else if (row != null) row.IsEnabled = item.Enabled.Value;
            else db.CampaignBaseOverrides.Add(new CampaignBaseOverride { Id = Guid.NewGuid(), CampaignId = q.CampaignId, System = req.System, Category = item.Category, ContentKey = item.Key, IsEnabled = item.Enabled.Value });
        }
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class ResetCampaignBaseHandler(IAppDbContext db) : ICommandHandler<ResetCampaignBaseCommand, Unit>
{
    public async Task<Unit> Handle(ResetCampaignBaseCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        db.CampaignBaseOverrides.RemoveRange(await db.CampaignBaseOverrides.Where(x => x.CampaignId == q.CampaignId && x.System == q.System).ToListAsync(ct));
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class SaveCampaignRestrictionsHandler(IAppDbContext db) : ICommandHandler<SaveCampaignRestrictionsCommand, HomebrewPackListItemDto>
{
    public async Task<HomebrewPackListItemDto> Handle(SaveCampaignRestrictionsCommand q, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var baseRows = await new GetCampaignBaseHandler(db).Handle(new(q.UserId, q.CampaignId, q.Request.System), ct);
        var name = q.Request.Name ?? $"Ограничения «{campaign.Name}»";
        CreatePackHandler.Validate(name, null);
        var pack = new HomebrewPack { Id = Guid.NewGuid(), OwnerUserId = q.UserId, Name = name.Trim(), System = q.Request.System };
        db.HomebrewPacks.Add(pack);
        var excluded = baseRows.Where(x => !x.Enabled).ToList();
        db.HomebrewPackExclusions.AddRange(excluded.Select(x => new HomebrewPackExclusion { Id = Guid.NewGuid(), HomebrewPackId = pack.Id, Category = x.Category, ContentKey = x.Key }));
        var otherLinks = await (from l in db.HomebrewPackCampaigns join p in db.HomebrewPacks on l.HomebrewPackId equals p.Id
            where l.CampaignId == q.CampaignId && p.OwnerUserId == q.UserId && p.System == pack.System
                && !db.HomebrewPackEntries.Any(e => e.HomebrewPackId == p.Id) && db.HomebrewPackExclusions.Any(e => e.HomebrewPackId == p.Id)
            select l).ToListAsync(ct);
        foreach (var link in otherLinks) link.IsEnabled = false;
        db.HomebrewPackCampaigns.Add(new HomebrewPackCampaign { Id = Guid.NewGuid(), CampaignId = q.CampaignId, HomebrewPackId = pack.Id, IsEnabled = true });
        var existingOverrides = await db.CampaignBaseOverrides.Where(x => x.CampaignId == q.CampaignId && x.System == pack.System).ToListAsync(ct);
        // Preserve explicit restorations against still-enabled mixed/other-author packs.
        var remainingPackIds = await (from l in db.HomebrewPackCampaigns join p in db.HomebrewPacks on l.HomebrewPackId equals p.Id
            where l.CampaignId == q.CampaignId && l.IsEnabled && l.Status == ContentConnectionStatus.Active && p.System == pack.System select p.Id).ToListAsync(ct);
        remainingPackIds.RemoveAll(id => otherLinks.Any(l => l.HomebrewPackId == id));
        var stillExcluded = await db.HomebrewPackExclusions.Where(x => remainingPackIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        var restoredRows = baseRows.Where(x => x.Enabled && stillExcluded.Any(e => e.Category == x.Category && e.ContentKey == x.Key)).ToList();
        foreach (var existing in existingOverrides)
            if (restoredRows.Any(x => x.Category == existing.Category && x.Key == existing.ContentKey)) existing.IsEnabled = true;
            else db.CampaignBaseOverrides.Remove(existing);
        foreach (var row in restoredRows.Where(x => !existingOverrides.Any(e => e.Category == x.Category && e.ContentKey == x.Key)))
            db.CampaignBaseOverrides.Add(new CampaignBaseOverride { Id = Guid.NewGuid(), CampaignId = q.CampaignId, System = pack.System, Category = row.Category, ContentKey = row.Key, IsEnabled = true });
        await db.SaveChangesAsync(ct);
        return HomebrewPackMapper.ToListItem(pack, 0) with { ExclusionCount = excluded.Count, Campaigns = [new(campaign.Id, campaign.Name)] };
    }
}
public class DisconnectCampaignPackHandler(IAppDbContext db) : ICommandHandler<DisconnectCampaignPackCommand, Unit>
{
    public async Task<Unit> Handle(DisconnectCampaignPackCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var link = await db.HomebrewPackCampaigns.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.HomebrewPackId == q.PackId, ct)
            ?? throw new DomainRuleException("Набор не подключён к кампании.");
        db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates.Where(x => x.HomebrewPackCampaignId == link.Id).ToListAsync(ct));
        db.HomebrewPackCampaigns.Remove(link);
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class SetCampaignPackEntriesHandler(IAppDbContext db) : ICommandHandler<SetCampaignPackEntriesCommand, Unit>
{
    public async Task<Unit> Handle(SetCampaignPackEntriesCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var link = await db.HomebrewPackCampaigns.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.HomebrewPackId == q.PackId, ct)
            ?? throw new DomainRuleException("Набор не подключён к кампании.");
        var entries = await db.HomebrewPackEntries.Where(x => x.HomebrewPackId == q.PackId).ToListAsync(ct);
        if (q.Request.Entries.Any(r => !entries.Any(x => x.EntryType == r.EntryType && x.EntryId == r.EntryId)))
            throw new DomainRuleException("Элемент не входит в подключённый набор.");
        var states = await db.CampaignPackEntryStates.Where(x => x.HomebrewPackCampaignId == link.Id).ToListAsync(ct);
        foreach (var item in q.Request.Entries.GroupBy(x => (x.EntryType, x.EntryId)).Select(g => g.Last()))
        {
            var state = states.SingleOrDefault(x => x.EntryType == item.EntryType && x.EntryId == item.EntryId);
            if (item.Enabled) { if (state != null) db.CampaignPackEntryStates.Remove(state); }
            else if (state != null) state.State = PackEntryState.Disabled;
            else db.CampaignPackEntryStates.Add(new CampaignPackEntryState { Id = Guid.NewGuid(), HomebrewPackCampaignId = link.Id, EntryType = item.EntryType, EntryId = item.EntryId, State = PackEntryState.Disabled });
        }
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class ConnectCampaignItemsHandler(IAppDbContext db) : ICommandHandler<ConnectCampaignItemsCommand, Unit>
{
    public async Task<Unit> Handle(ConnectCampaignItemsCommand q, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var definitions = await ContentDefinitions.LoadAsync(db, ct: ct, ids:q.Request.Entries.Select(x => x.EntryId).ToList());
        var owners = await db.CampaignMembers.Where(x => x.CampaignId == campaign.Id).Select(x => x.UserId).ToListAsync(ct);
        owners.Add(campaign.GmUserId);
        var requested = q.Request.Entries.Distinct().ToList();
        if (requested.Any(r => !definitions.Any(x => x.Type == r.EntryType && x.Id == r.EntryId && x.OwnerUserId != null && owners.Contains(x.OwnerUserId.Value))))
            throw new DomainRuleException("Подключить можно только контент мастера или текущего участника кампании.");
        var rows = await db.CampaignContentItems.Where(x => x.CampaignId == campaign.Id).ToListAsync(ct);
        foreach (var item in requested)
        {
            var row = rows.SingleOrDefault(x => x.EntryType == item.EntryType && x.EntryId == item.EntryId);
            if (row != null) { row.IsEnabled = true; row.Status = ContentConnectionStatus.Active; }
            else db.CampaignContentItems.Add(new CampaignContentItem { Id = Guid.NewGuid(), CampaignId = campaign.Id, EntryType = item.EntryType, EntryId = item.EntryId });
        }
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class SetCampaignItemHandler(IAppDbContext db) : ICommandHandler<SetCampaignItemCommand, Unit>
{
    public async Task<Unit> Handle(SetCampaignItemCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var row = await db.CampaignContentItems.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.Id == q.ItemId, ct)
            ?? throw new DomainRuleException("Элемент не подключён.");
        row.IsEnabled = q.IsEnabled;
        if (q.IsEnabled) row.Status = ContentConnectionStatus.Active;
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class RemoveCampaignItemHandler(IAppDbContext db) : ICommandHandler<RemoveCampaignItemCommand, Unit>
{
    public async Task<Unit> Handle(RemoveCampaignItemCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        var row = await db.CampaignContentItems.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.Id == q.ItemId, ct)
            ?? throw new DomainRuleException("Элемент не подключён.");
        db.CampaignContentItems.Remove(row);
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
