using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record DuplicateCharacterCommand(Guid UserId, Guid CharacterId) : ICommand<Guid>;
