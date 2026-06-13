using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.CustomContent;

public record DeleteCustomHeroicAbilityCommand(Guid UserId, Guid HeroicAbilityId) : ICommand<Unit>;
