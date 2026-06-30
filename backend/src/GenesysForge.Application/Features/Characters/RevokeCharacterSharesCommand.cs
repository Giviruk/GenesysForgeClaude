using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RevokeCharacterSharesCommand(Guid UserId, Guid CharacterId) : ICommand<Unit>;
