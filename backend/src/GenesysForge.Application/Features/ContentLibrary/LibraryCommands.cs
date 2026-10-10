using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.HomebrewPacks;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentLibrary;

public record CreatePackCommand(Guid UserId, PackDetailsRequest Request) : ICommand<HomebrewPackListItemDto>;
public record UpdatePackCommand(Guid UserId, Guid PackId, UpdatePackDetailsRequest Request) : ICommand<Unit>;
public record DeletePackCommand(Guid UserId, Guid PackId) : ICommand<Unit>;
public record ChangePackEntriesCommand(Guid UserId, Guid PackId, ContentEntriesRequest Request, bool Remove) : ICommand<Unit>;
public record ChangePackExclusionsCommand(Guid UserId, Guid PackId, PackExclusionsRequest Request, bool Remove) : ICommand<Unit>;

public class CreatePackHandler(IAppDbContext db) : ICommandHandler<CreatePackCommand, HomebrewPackListItemDto>
{
    internal static void Validate(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200 || description?.Length > 2000)
            throw new DomainRuleException("Укажите название набора до 200 символов и описание до 2000 символов.");
        if (description?.StartsWith("Personal custom:") == true || description?.StartsWith("Campaign custom:") == true)
            throw new DomainRuleException("Описание содержит зарезервированный служебный маркер.");
    }
    public async Task<HomebrewPackListItemDto> Handle(CreatePackCommand q, CancellationToken ct = default)
    {
        Validate(q.Request.Name, q.Request.Description);
        if (!Enum.IsDefined(q.Request.System)) throw new DomainRuleException("Неизвестная игровая система.");
        var pack = new HomebrewPack { Id = Guid.NewGuid(), OwnerUserId = q.UserId, System = q.Request.System, Name = q.Request.Name.Trim(), Description = q.Request.Description?.Trim() ?? "" };
        db.HomebrewPacks.Add(pack);
        await db.SaveChangesAsync(ct);
        return HomebrewPackMapper.ToListItem(pack, 0);
    }
}
public class UpdatePackHandler(IAppDbContext db) : ICommandHandler<UpdatePackCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePackCommand q, CancellationToken ct = default)
    {
        CreatePackHandler.Validate(q.Request.Name, q.Request.Description);
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, q.PackId, ct, true);
        var before = new { pack.Name, pack.Description };
        pack.Name = q.Request.Name.Trim(); pack.Description = q.Request.Description?.Trim() ?? ""; pack.UpdatedAt = DateTime.UtcNow;
        CustomContentAudit.Updated(db, "pack", pack.Id, pack.Id, pack.Name, q.UserId, before, new { pack.Name, pack.Description });
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class DeletePackHandler(IAppDbContext db) : ICommandHandler<DeletePackCommand, Unit>
{
    public async Task<Unit> Handle(DeletePackCommand q, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, q.PackId, ct, true);
        var links = await db.HomebrewPackCampaigns.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct);
        var ids = links.Select(x => x.Id).ToList();
        db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates.Where(x => ids.Contains(x.HomebrewPackCampaignId)).ToListAsync(ct));
        db.HomebrewPackCampaigns.RemoveRange(links);
        db.HomebrewPackCharacters.RemoveRange(await db.HomebrewPackCharacters.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct));
        db.HomebrewPackEntries.RemoveRange(await db.HomebrewPackEntries.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct));
        db.HomebrewPackExclusions.RemoveRange(await db.HomebrewPackExclusions.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct));
        // Deprecated pointers may survive the migration for named packs; they never control visibility.
        foreach (var x in await db.SkillDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.TalentDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.ItemDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.ArchetypeDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.CareerDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.HeroicAbilityDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.AttachmentDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        foreach (var x in await db.MountDefs.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct)) x.HomebrewPackId = null;
        db.HomebrewPacks.Remove(pack);
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class ChangePackEntriesHandler(IAppDbContext db) : ICommandHandler<ChangePackEntriesCommand, Unit>
{
    public async Task<Unit> Handle(ChangePackEntriesCommand q, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, q.PackId, ct, true);
        var requested = q.Request.Entries.Distinct().ToList();
        if (requested.Count > 1000) throw new DomainRuleException("Слишком много элементов в запросе.");
        var own = await ContentDefinitions.LoadAsync(db, pack.System, q.UserId, ct);
        if (requested.Any(e => !own.Any(x => x.Type == e.EntryType && x.Id == e.EntryId)))
            throw new DomainRuleException("Можно выбирать только свой контент той же системы.");
        var current = await db.HomebrewPackEntries.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct);
        foreach (var entry in requested)
        {
            var row = current.FirstOrDefault(x => x.EntryType == entry.EntryType && x.EntryId == entry.EntryId);
            if (q.Remove && row != null)
            {
                db.HomebrewPackEntries.Remove(row);
                var links = await db.HomebrewPackCampaigns.Where(x => x.HomebrewPackId == pack.Id).Select(x => x.Id).ToListAsync(ct);
                db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates.Where(x => links.Contains(x.HomebrewPackCampaignId) && x.EntryType == entry.EntryType && x.EntryId == entry.EntryId).ToListAsync(ct));
            }
            else if (!q.Remove && row == null) await ContentMembership.AddAsync(db, pack.Id, entry.EntryType, entry.EntryId, ct);
            else continue;
            var def = own.Single(x => x.Type == entry.EntryType && x.Id == entry.EntryId);
            db.CustomContentChanges.Add(new CustomContentChange { Id = Guid.NewGuid(), HomebrewPackId = pack.Id, DefinitionId = def.Id, DefinitionType = entry.EntryType.ToString(), DefinitionName = def.Name, UserId = q.UserId,
                Action = q.Remove ? CustomContentChangeAction.RemovedFromPack : CustomContentChangeAction.AddedToPack });
            pack.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class ChangePackExclusionsHandler(IAppDbContext db) : ICommandHandler<ChangePackExclusionsCommand, Unit>
{
    public async Task<Unit> Handle(ChangePackExclusionsCommand q, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, q.PackId, ct, true);
        var requested = q.Request.Items.Distinct().ToList();
        var catalog = await BaseCatalog.LoadAsync(db, pack.System, ct);
        if (!q.Remove && requested.Any(r => !catalog.Any(x => x.Category == r.Category && x.Key == r.Key)))
            throw new DomainRuleException("Ограничение должно ссылаться на встроенный контент системы набора.");
        var rows = await db.HomebrewPackExclusions.Where(x => x.HomebrewPackId == pack.Id).ToListAsync(ct);
        var before = rows.Select(x => new BaseContentRef(x.Category, x.ContentKey)).OrderBy(x => x.Category).ThenBy(x => x.Key).ToList();
        foreach (var item in requested)
        {
            var row = rows.FirstOrDefault(x => x.Category == item.Category && x.ContentKey == item.Key);
            if (q.Remove && row != null) { db.HomebrewPackExclusions.Remove(row); rows.Remove(row); }
            else if (!q.Remove && row == null)
            {
                row = new HomebrewPackExclusion { Id = Guid.NewGuid(), HomebrewPackId = pack.Id, Category = item.Category, ContentKey = item.Key };
                db.HomebrewPackExclusions.Add(row); rows.Add(row);
            }
        }
        var after = rows.Select(x => new BaseContentRef(x.Category, x.ContentKey)).OrderBy(x => x.Category).ThenBy(x => x.Key).ToList();
        CustomContentAudit.Updated(db, "pack", pack.Id, pack.Id, pack.Name, q.UserId, new { Exclusions = before }, new { Exclusions = after });
        pack.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
