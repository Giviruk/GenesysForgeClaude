using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>История персонажа (audit log), новые записи первыми. Доступна только владельцу.</summary>
public record GetCharacterAuditQuery(Guid UserId, Guid CharacterId, int Take)
    : IQuery<IReadOnlyList<CharacterAuditEntryDto>>;

public class GetCharacterAuditHandler(IAppDbContext db)
    : IQueryHandler<GetCharacterAuditQuery, IReadOnlyList<CharacterAuditEntryDto>>
{
    public async Task<IReadOnlyList<CharacterAuditEntryDto>> Handle(
        GetCharacterAuditQuery query, CancellationToken ct = default)
    {
        // Проверка владельца (бросит, если персонаж чужой/не найден). Откату нужны только навыки и
        // таланты — полный граф здесь стоил ~18 SQL на каждое открытие истории.
        var character = await db.AuditUndoQuery()
            .FirstOrDefaultAsync(c => c.Id == query.CharacterId && c.OwnerUserId == query.UserId, ct)
            ?? throw new DomainRuleException("Персонаж не найден.");
        var take = Math.Clamp(query.Take, 1, 500);

        var entries = await db.CharacterAuditEntries.AsNoTracking()
            .Where(a => a.CharacterId == query.CharacterId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return entries
            .Select(a => new CharacterAuditEntryDto(
                a.Id, a.CreatedAt, a.Action, a.Summary, a.XpDelta, a.TotalXpAfter, a.SpentXpAfter,
                CharacterAuditUndo.CanUndo(a, character, entries)))
            .ToList();
    }
}
