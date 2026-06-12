using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record UpdateItemCommand(Guid UserId, Guid CharacterId, Guid ItemId, UpdateItemRequest Request) : ICommand<Unit>;
