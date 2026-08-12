using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomCareerCommand(Guid UserId, Guid CampaignId, CreateCustomCareerRequest Request) : ICommand<CareerDto>;
