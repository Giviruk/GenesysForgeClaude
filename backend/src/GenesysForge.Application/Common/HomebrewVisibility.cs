using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

public static class HomebrewVisibility
{
    public static async Task<HashSet<Guid>> GetVisiblePackIdsAsync(
        IAppDbContext db,
        Guid userId,
        GameSystem system,
        Guid? characterId = null,
        Guid? campaignId = null,
        CancellationToken ct = default)
    {
        if (characterId is not null)
        {
            var ownsCharacter = await db.Characters.AsNoTracking()
                .AnyAsync(c => c.Id == characterId.Value && c.OwnerUserId == userId, ct);
            if (!ownsCharacter) throw new DomainRuleException("Персонаж не найден.");
        }

        if (campaignId is not null)
            await CampaignMapper.GetAccessibleAsync(db, userId, campaignId.Value, ct);

        var campaignIds = characterId is not null
            ? await db.CampaignCharacters.AsNoTracking()
                .Where(x => x.CharacterId == characterId.Value)
                .Select(x => x.CampaignId).ToListAsync(ct)
            : [];
        // An explicit campaign is its own reference context. Otherwise use the character's campaigns.
        if (campaignId is not null) campaignIds = [campaignId.Value];
        if (campaignIds.Count > 0)
        {
            var campaignPacks = await (from link in db.HomebrewPackCampaigns.AsNoTracking()
                join pack in db.HomebrewPacks.AsNoTracking() on link.HomebrewPackId equals pack.Id
                where campaignIds.Contains(link.CampaignId) && link.IsEnabled && pack.System == system
                select pack.Id).ToListAsync(ct);
            return campaignPacks.ToHashSet();
        }

        var personalPacks = await db.HomebrewPacks.AsNoTracking()
            .Where(p => p.OwnerUserId == userId && p.System == system)
            .Select(p => new { p.Id, p.IsEnabledByDefault })
            .ToListAsync(ct);
        var visible = personalPacks.Where(p => p.IsEnabledByDefault).Select(p => p.Id).ToHashSet();
        var personalPackIds = personalPacks.Select(p => p.Id).ToHashSet();
        if (characterId is not null)
        {
            var toggles = await db.HomebrewPackCharacters.AsNoTracking()
                .Where(x => x.CharacterId == characterId.Value && personalPackIds.Contains(x.HomebrewPackId))
                .Select(x => new { x.HomebrewPackId, x.IsEnabled })
                .ToListAsync(ct);
            ApplyToggles(visible, toggles.Select(x => (x.HomebrewPackId, x.IsEnabled)));
        }

        return visible;
    }

    public static bool IsVisibleCustom(Guid ownerUserId, Guid? packId, Guid userId, HashSet<Guid> visiblePackIds) =>
        packId is null ? ownerUserId == userId : visiblePackIds.Contains(packId.Value);

    private static void ApplyToggles(HashSet<Guid> visible, IEnumerable<(Guid PackId, bool IsEnabled)> toggles)
    {
        foreach (var toggle in toggles)
        {
            if (toggle.IsEnabled) visible.Add(toggle.PackId);
            else visible.Remove(toggle.PackId);
        }
    }
}
