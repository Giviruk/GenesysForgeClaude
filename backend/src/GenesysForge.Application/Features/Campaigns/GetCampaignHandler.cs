using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public class GetCampaignHandler(IAppDbContext db) : IQueryHandler<GetCampaignQuery, CampaignDetailDto>
{
    public async Task<CampaignDetailDto> Handle(GetCampaignQuery query, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);
        return await CampaignMapper.BuildDetailAsync(db, campaign, query.UserId, ct);
    }
}
