using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class GetCampaignsHandler(IAppDbContext db) : IQueryHandler<GetCampaignsQuery, List<CampaignListItemDto>>
{
    public async Task<List<CampaignListItemDto>> Handle(GetCampaignsQuery query, CancellationToken ct = default)
    {
        var uid = query.UserId;

        var campaigns = await db.Campaigns.AsNoTracking()
            // Коррелированный EXISTS оставляет проверку членства в одном SQL-запросе и не переносит
            // все campaign ids пользователя в память отдельным round trip.
            .Where(c => c.GmUserId == uid || db.CampaignCharacters.Any(
                cc => cc.PlayerUserId == uid && cc.CampaignId == c.Id))
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
