using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record DeleteCharacterCommand(Guid UserId, Guid CharacterId) : ICommand<Unit>;
