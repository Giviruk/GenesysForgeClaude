using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RefundSkillRankCommand(Guid UserId, Guid CharacterId, Guid SkillDefId) : ICommand<Unit>;
