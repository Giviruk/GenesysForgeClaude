using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record BuyServiceCommand(Guid UserId, Guid CharacterId, BuyServiceRequest Request)
    : ICommand<Unit>;
