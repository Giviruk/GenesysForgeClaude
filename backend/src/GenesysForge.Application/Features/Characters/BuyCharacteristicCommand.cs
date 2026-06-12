using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public record BuyCharacteristicCommand(Guid UserId, Guid CharacterId, CharacteristicType Characteristic) : ICommand<Unit>;
