using GenesysForge.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

public static class ContentNotificationTargets
{
    public static async Task<List<Guid>> ForAuthorAsync(IAppDbContext db, Guid userId, CancellationToken ct)
    {
        var packs = await (from link in db.HomebrewPackCampaigns.AsNoTracking() join pack in db.HomebrewPacks.AsNoTracking() on link.HomebrewPackId equals pack.Id
            where pack.OwnerUserId == userId select link.CampaignId).ToListAsync(ct);
        var ownedIds = db.SkillDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id)
            .Concat(db.TalentDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id))
            .Concat(db.ItemDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id))
            .Concat(db.ArchetypeDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id))
            .Concat(db.CareerDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id))
            .Concat(db.HeroicAbilityDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id))
            .Concat(db.AttachmentDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id))
            .Concat(db.MountDefs.Where(x => x.OwnerUserId == userId).Select(x => x.Id));
        packs.AddRange(await db.CampaignContentItems.Where(x => ownedIds.Contains(x.EntryId)).Select(x => x.CampaignId).ToListAsync(ct));
        return packs.Distinct().ToList();
    }
}
