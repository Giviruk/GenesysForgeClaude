using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public record JoinCampaignCommand(Guid UserId, JoinCampaignRequest Request) : ICommand<CampaignDetailDto>;
