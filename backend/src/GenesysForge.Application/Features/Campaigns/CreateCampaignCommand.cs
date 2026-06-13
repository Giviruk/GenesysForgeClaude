using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public record CreateCampaignCommand(Guid UserId, CreateCampaignRequest Request) : ICommand<CampaignDetailDto>;
