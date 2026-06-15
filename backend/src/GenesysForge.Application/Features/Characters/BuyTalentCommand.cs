using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public record BuyTalentCommand(
    Guid UserId, Guid CharacterId, Guid TalentDefId, CharacteristicType? Characteristic = null) : ICommand<Unit>;
