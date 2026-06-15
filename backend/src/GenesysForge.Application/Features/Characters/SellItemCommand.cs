using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record SellItemCommand(Guid UserId, Guid CharacterId, Guid ItemId, SellItemRequest Request) : ICommand<Unit>;
