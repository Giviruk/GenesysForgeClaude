using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class UndoCharacterAuditHandler(
    IAppDbContext db,
    ICommandHandler<RefundSkillRankCommand, Unit> refundSkillRank,
    ICommandHandler<RefundTalentCommand, Unit> refundTalent)
    : ICommandHandler<UndoCharacterAuditCommand, Unit>
{
    public async Task<Unit> Handle(UndoCharacterAuditCommand command, CancellationToken ct = default)
    {
        var character = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var entry = await db.CharacterAuditEntries.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == command.AuditEntryId
                && value.CharacterId == command.CharacterId, ct)
            ?? throw new DomainRuleException("Запись истории не найдена.");
        var history = await db.CharacterAuditEntries.AsNoTracking()
            .Where(value => value.CharacterId == command.CharacterId)
            .OrderByDescending(value => value.CreatedAt)
            .ToListAsync(ct);

        if (!CharacterAuditUndo.TryResolve(entry, character, history, out var target))
            throw new DomainRuleException(
                "Эту покупку нельзя отменить: она уже не является последним актуальным действием или нарушает правила листа.");

        if (target.IsTalent)
        {
            await refundTalent.Handle(new RefundTalentCommand(
                command.UserId, command.CharacterId, target.DefinitionId,
                RevertedAuditId: entry.Id, ExpectedRank: target.ExpectedRank, AllowAfterCreation: true), ct);
        }
        else
        {
            await refundSkillRank.Handle(new RefundSkillRankCommand(
                command.UserId, command.CharacterId, target.DefinitionId,
                RevertedAuditId: entry.Id, ExpectedRank: target.ExpectedRank, AllowAfterCreation: true), ct);
        }

        return Unit.Value;
    }
}
