using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record SetHeroicAbilityCommand(Guid UserId, Guid CharacterId, Guid? HeroicAbilityId) : ICommand<Unit>;
