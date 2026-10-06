using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

internal static class CampaignCustomContent
{
    public static async Task<Guid> GetOrCreatePackIdAsync(
        IAppDbContext db, Guid? campaignId, Guid userId, GameSystem system, CancellationToken ct)
    {
        if (campaignId is null)
        {
            var marker = $"Personal custom:{system}";
            var personal = await db.HomebrewPacks.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).FirstOrDefaultAsync(
                p => p.OwnerUserId == userId && p.System == system && p.Description == marker, ct);
            if (personal is not null) return personal.Id;
            var pack = new HomebrewPack
            {
                Id = Guid.NewGuid(), OwnerUserId = userId, System = system,
                Name = $"Моя библиотека ({system})", Description = marker, IsEnabledByDefault = true,
            };
            db.HomebrewPacks.Add(pack);
            await db.SaveChangesAsync(ct);
            return pack.Id;
        }

        var campaign = await db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new DomainRuleException("Кампания не найдена.");
        if (campaign.GmUserId != userId)
            throw new DomainRuleException("Только мастер кампании может создавать кастомный контент.");

        var existing = await (from link in db.HomebrewPackCampaigns
            join pack in db.HomebrewPacks on link.HomebrewPackId equals pack.Id
            where link.CampaignId == campaignId && pack.System == system && pack.OwnerUserId == userId
                && pack.Description == PackMarker(campaignId.Value)
            orderby pack.CreatedAt, pack.Id
            select new { pack.Id, Link = link }).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (!existing.Link.IsEnabled)
            {
                existing.Link.IsEnabled = true;
                existing.Link.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return existing.Id;
        }

        var createdPack = new HomebrewPack
        {
            Id = Guid.NewGuid(), OwnerUserId = userId, System = system,
            Name = $"{campaign.Name} — кастом ({system})",
            Description = PackMarker(campaignId.Value), IsEnabledByDefault = true,
        };
        db.HomebrewPacks.Add(createdPack);
        db.HomebrewPackCampaigns.Add(new HomebrewPackCampaign
        {
            Id = Guid.NewGuid(), HomebrewPackId = createdPack.Id, CampaignId = campaignId.Value, IsEnabled = true,
        });
        await db.SaveChangesAsync(ct);
        return createdPack.Id;
    }

    private static string PackMarker(Guid campaignId) => $"Campaign custom:{campaignId:N}";
}
