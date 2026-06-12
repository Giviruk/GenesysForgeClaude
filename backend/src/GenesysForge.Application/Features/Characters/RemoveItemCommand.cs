using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RemoveItemCommand(Guid UserId, Guid CharacterId, Guid ItemId) : ICommand<Unit>;
