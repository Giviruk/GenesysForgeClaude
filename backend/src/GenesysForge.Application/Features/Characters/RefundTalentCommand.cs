using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RefundTalentCommand(Guid UserId, Guid CharacterId, Guid TalentDefId) : ICommand<Unit>;
