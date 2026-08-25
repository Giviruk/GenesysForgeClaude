using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RefundSkillRankCommand(
    Guid UserId,
    Guid CharacterId,
    Guid SkillDefId,
    Guid? RevertedAuditId = null,
    int? ExpectedRank = null,
    bool AllowAfterCreation = false) : ICommand<Unit>;
