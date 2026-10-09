using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

public static class ContentMembership
{
    /// <summary>Validate first, then enqueue changes in the definition's unit of work.</summary>
    public static async Task ApplyAsync(IAppDbContext db, Guid userId, GameSystem system, CustomEntryType type,
        Guid id, IReadOnlyList<Guid>? packIds, Guid? campaignId, CancellationToken ct)
    {
        var current = await db.HomebrewPackEntries.Where(x => x.EntryType == type && x.EntryId == id).ToListAsync(ct);
        var requested = packIds?.Distinct().ToHashSet() ?? current.Select(x => x.HomebrewPackId).ToHashSet();
        var packs = await db.HomebrewPacks.Where(x => requested.Contains(x.Id)).ToListAsync(ct);
        if (packs.Count != requested.Count || packs.Any(x => x.OwnerUserId != userId || x.System != system))
            throw new DomainRuleException("Элемент можно добавить только в собственный набор той же системы.");
        if (campaignId is not null) await CampaignMapper.GetAsGmAsync(db, userId, campaignId.Value, ct);
        if (packIds is not null)
        {
            var name = type switch
            {
                CustomEntryType.Skill => await db.SkillDefs.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct),
                CustomEntryType.Talent => await db.TalentDefs.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct),
                CustomEntryType.Item => await db.ItemDefs.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct),
                CustomEntryType.Archetype => await db.ArchetypeDefs.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct),
                CustomEntryType.Career => await db.CareerDefs.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct),
                CustomEntryType.HeroicAbility => await db.HeroicAbilityDefs.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(ct),
                _ => null,
            };
            var removed = current.Where(x => !requested.Contains(x.HomebrewPackId)).ToList();
            db.HomebrewPackEntries.RemoveRange(removed);
            var removedPackIds = removed.Select(x => x.HomebrewPackId).ToList();
            var removedLinks = await db.HomebrewPackCampaigns.Where(x => removedPackIds.Contains(x.HomebrewPackId)).Select(x => x.Id).ToListAsync(ct);
            db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates.Where(x => removedLinks.Contains(x.HomebrewPackCampaignId) && x.EntryType == type && x.EntryId == id).ToListAsync(ct));
            foreach (var entry in removed)
            {
                if (name is not null) CustomContentAudit.MembershipChanged(db, type, id, entry.HomebrewPackId, name, userId, true);
                var removedPack = await db.HomebrewPacks.FirstAsync(x => x.Id == entry.HomebrewPackId, ct);
                removedPack.UpdatedAt = DateTime.UtcNow;
            }
            foreach (var pack in packs.Where(p => !current.Any(x => x.HomebrewPackId == p.Id)))
            {
                await AddAsync(db, pack.Id, type, id, ct);
                if (name is not null) CustomContentAudit.MembershipChanged(db, type, id, pack.Id, name, userId, false);
                pack.UpdatedAt = DateTime.UtcNow;
            }
        }
        if (campaignId is not null && !await db.CampaignContentItems.AnyAsync(x => x.CampaignId == campaignId && x.EntryType == type && x.EntryId == id, ct))
            db.CampaignContentItems.Add(new CampaignContentItem { Id = Guid.NewGuid(), CampaignId = campaignId.Value, EntryType = type, EntryId = id });
    }

    public static async Task AddAsync(IAppDbContext db, Guid packId, CustomEntryType type, Guid id, CancellationToken ct)
    {
        db.HomebrewPackEntries.Add(new HomebrewPackEntry { Id = Guid.NewGuid(), HomebrewPackId = packId, EntryType = type, EntryId = id });
        foreach (var link in await db.HomebrewPackCampaigns.Where(x => x.HomebrewPackId == packId && x.UpdatePolicy == ContentUpdatePolicy.Manual).ToListAsync(ct))
            db.CampaignPackEntryStates.Add(new CampaignPackEntryState { Id = Guid.NewGuid(), HomebrewPackCampaignId = link.Id, EntryType = type, EntryId = id, State = PackEntryState.Pending });
    }

    public static async Task RemoveDefinitionAsync(IAppDbContext db, CustomEntryType type, Guid id, CancellationToken ct)
    {
        db.HomebrewPackEntries.RemoveRange(await db.HomebrewPackEntries.Where(x => x.EntryType == type && x.EntryId == id).ToListAsync(ct));
        db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates.Where(x => x.EntryType == type && x.EntryId == id).ToListAsync(ct));
        db.CampaignContentItems.RemoveRange(await db.CampaignContentItems.Where(x => x.EntryType == type && x.EntryId == id).ToListAsync(ct));
    }
}
