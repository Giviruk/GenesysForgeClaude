using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RemoveCriticalInjuryCommand(Guid UserId, Guid CharacterId, Guid InjuryId) : ICommand<Unit>;
