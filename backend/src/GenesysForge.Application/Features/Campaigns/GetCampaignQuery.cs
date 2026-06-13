using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public record GetCampaignQuery(Guid UserId, Guid CampaignId) : IQuery<CampaignDetailDto>;
