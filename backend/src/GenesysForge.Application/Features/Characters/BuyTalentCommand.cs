using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record BuyTalentCommand(Guid UserId, Guid CharacterId, Guid TalentDefId) : ICommand<Unit>;
