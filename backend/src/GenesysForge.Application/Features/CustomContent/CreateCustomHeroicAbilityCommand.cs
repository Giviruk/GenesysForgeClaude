using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomHeroicAbilityCommand(Guid UserId, CreateCustomHeroicAbilityRequest Request)
    : ICommand<HeroicAbilityDto>;
