using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record CreateCharacterCommand(Guid UserId, CreateCharacterRequest Request) : ICommand<Guid>;
