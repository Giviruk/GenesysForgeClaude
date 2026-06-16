using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Текущая активная сцена кампании (или null). Доступна GM и участникам кампании.</summary>
public record GetSessionQuery(Guid UserId, Guid CampaignId) : IQuery<GameSessionDto?>;

public class GetSessionHandler(IAppDbContext db) : IQueryHandler<GetSessionQuery, GameSessionDto?>
{
    public async Task<GameSessionDto?> Handle(GetSessionQuery query, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);
        var session = await GameTableMapper.LoadActiveAsync(db, campaign.Id, ct);
        return session is null ? null : GameTableMapper.ToDto(session, campaign.GmUserId == query.UserId);
    }
}
