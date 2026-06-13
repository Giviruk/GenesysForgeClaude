using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record UpdateCustomHeroicAbilityCommand(Guid UserId, Guid HeroicAbilityId, CreateCustomHeroicAbilityRequest Request)
    : ICommand<HeroicAbilityDto>;
