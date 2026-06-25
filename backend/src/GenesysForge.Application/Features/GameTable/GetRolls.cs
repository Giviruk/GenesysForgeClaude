using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Последние броски кампании (новые первыми). Секретные видит только GM.</summary>
public record GetRollsQuery(Guid UserId, Guid CampaignId, int Take) : IQuery<IReadOnlyList<RollLogEntryDto>>;

public class GetRollsHandler(IAppDbContext db) : IQueryHandler<GetRollsQuery, IReadOnlyList<RollLogEntryDto>>
{
    public async Task<IReadOnlyList<RollLogEntryDto>> Handle(GetRollsQuery query, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);
        var isGm = campaign.GmUserId == query.UserId;
        var take = Math.Clamp(query.Take, 1, 100);

        return await db.RollLogEntries.AsNoTracking()
            .Where(r => r.CampaignId == campaign.Id && (isGm || !r.IsSecret))
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .Select(r => new RollLogEntryDto(
                r.Id, r.CampaignId, r.SessionId, r.ActorName, r.Label,
                r.PoolJson, r.ResultJson, r.Summary, r.IsSecret, r.CreatedAt))
            .ToListAsync(ct);
    }
}
