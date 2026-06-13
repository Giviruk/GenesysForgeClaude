using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public record GetCampaignsQuery(Guid UserId) : IQuery<List<CampaignListItemDto>>;
