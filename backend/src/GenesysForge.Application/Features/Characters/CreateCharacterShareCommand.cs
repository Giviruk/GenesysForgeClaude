using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record CreateCharacterShareCommand(Guid UserId, Guid CharacterId) : ICommand<CharacterShareResponse>;
