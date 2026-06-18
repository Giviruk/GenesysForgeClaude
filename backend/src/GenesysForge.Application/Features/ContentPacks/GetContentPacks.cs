using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentPacks;

/// <summary>Список Content Pack кампании. GM видит все, игрок — только опубликованные.</summary>
public record GetContentPacksQuery(Guid UserId, Guid CampaignId) : IQuery<List<ContentPackListItemDto>>;

public class GetContentPacksHandler(IAppDbContext db)
    : IQueryHandler<GetContentPacksQuery, List<ContentPackListItemDto>>
{
    public async Task<List<ContentPackListItemDto>> Handle(GetContentPacksQuery q, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, q.UserId, q.CampaignId, ct);
        var isGm = campaign.GmUserId == q.UserId;

        var packs = await db.ContentPacks.AsNoTracking()
            .Include(p => p.Entries)
            .Where(p => p.CampaignId == q.CampaignId && (isGm || p.IsPublicToCampaign))
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);
        return packs.Select(ContentPackMapper.ToListItem).ToList();
    }
}
