using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomHeroicAbilityCommand(Guid UserId, Guid CampaignId, CreateCustomHeroicAbilityRequest Request)
    : ICommand<HeroicAbilityDto>;
