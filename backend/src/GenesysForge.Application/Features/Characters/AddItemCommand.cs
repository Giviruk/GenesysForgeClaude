using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record AddItemCommand(Guid UserId, Guid CharacterId, AddItemRequest Request) : ICommand<Guid>;
