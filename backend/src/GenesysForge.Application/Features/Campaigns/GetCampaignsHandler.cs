using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class GetCampaignsHandler(IAppDbContext db) : IQueryHandler<GetCampaignsQuery, List<CampaignListItemDto>>
{
    public async Task<List<CampaignListItemDto>> Handle(GetCampaignsQuery query, CancellationToken ct = default)
    {
        var uid = query.UserId;

        // кампании, где я мастер, либо где участвует мой персонаж
        var memberCampaignIds = await db.CampaignCharacters.AsNoTracking()
            .Where(cc => cc.PlayerUserId == uid)
            .Select(cc => cc.CampaignId)
            .Distinct()
            .ToListAsync(ct);

        var campaigns = await db.Campaigns.AsNoTracking()
            .Where(c => c.GmUserId == uid || memberCampaignIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id, c.Name, c.GmUserId, c.CreatedAt,
                Count = db.CampaignCharacters.Count(cc => cc.CampaignId == c.Id),
            })
            .ToListAsync(ct);

        return campaigns
            .Select(c => new CampaignListItemDto(c.Id, c.Name, c.GmUserId == uid, c.Count, c.CreatedAt))
            .ToList();
    }
}
