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

        var packs = await db.HomebrewPacks.AsNoTracking()
            .Where(p => p.OwnerUserId == userId && p.System == system)
            .Select(p => new { p.Id, p.IsEnabledByDefault })
            .ToListAsync(ct);

        var visible = packs.Where(p => p.IsEnabledByDefault).Select(p => p.Id).ToHashSet();
        var packIds = packs.Select(p => p.Id).ToHashSet();

        if (characterId is not null)
        {
            var toggles = await db.HomebrewPackCharacters.AsNoTracking()
                .Where(x => x.CharacterId == characterId.Value && packIds.Contains(x.HomebrewPackId))
                .Select(x => new { x.HomebrewPackId, x.IsEnabled })
                .ToListAsync(ct);
            ApplyToggles(visible, toggles.Select(x => (x.HomebrewPackId, x.IsEnabled)));
        }

        if (campaignId is not null)
        {
            var toggles = await db.HomebrewPackCampaigns.AsNoTracking()
                .Where(x => x.CampaignId == campaignId.Value && packIds.Contains(x.HomebrewPackId))
                .Select(x => new { x.HomebrewPackId, x.IsEnabled })
                .ToListAsync(ct);
            ApplyToggles(visible, toggles.Select(x => (x.HomebrewPackId, x.IsEnabled)));
        }

        return visible;
    }

    public static bool IsVisibleCustom(Guid ownerUserId, Guid? packId, Guid userId, HashSet<Guid> visiblePackIds) =>
        ownerUserId == userId && (packId is null || visiblePackIds.Contains(packId.Value));

    private static void ApplyToggles(HashSet<Guid> visible, IEnumerable<(Guid PackId, bool IsEnabled)> toggles)
    {
        foreach (var toggle in toggles)
        {
            if (toggle.IsEnabled) visible.Add(toggle.PackId);
            else visible.Remove(toggle.PackId);
        }
    }
}
