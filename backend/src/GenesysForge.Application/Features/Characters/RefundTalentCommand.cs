using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record RefundTalentCommand(
    Guid UserId,
    Guid CharacterId,
    Guid TalentDefId,
    Guid? RevertedAuditId = null,
    int? ExpectedRank = null,
    bool AllowAfterCreation = false) : ICommand<Unit>;
