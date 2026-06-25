using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Выдача/коррекция суммарного XP с записью в историю (XpAwarded).</summary>
public record AwardXpCommand(Guid UserId, Guid CharacterId, AwardXpRequest Request) : ICommand<Unit>;

public class AwardXpHandler(IAppDbContext db) : ICommandHandler<AwardXpCommand, Unit>
{
    public async Task<Unit> Handle(AwardXpCommand command, CancellationToken ct = default)
    {
        var amount = command.Request.Amount;
        if (amount == 0) throw new DomainRuleException("Размер награды XP не может быть нулевым.");

        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.TotalXp + amount < c.SpentXp)
            throw new DomainRuleException($"Суммарный XP не может стать меньше потраченного ({c.SpentXp}).");

        c.TotalXp += amount;

        var note = command.Request.Note?.Trim();
        var sign = amount > 0 ? "+" : "";
        var summary = string.IsNullOrEmpty(note)
            ? $"Выдан XP: {sign}{amount}"
            : $"Выдан XP: {sign}{amount} — {note}";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.XpAwarded,
            summary, amount, new { amount, note });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
