using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
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
        // Проверка владельца (бросит, если персонаж чужой/не найден).
        await db.GetOwnedAsync(query.UserId, query.CharacterId, tracking: false, ct);
        var take = Math.Clamp(query.Take, 1, 500);

        return await db.CharacterAuditEntries.AsNoTracking()
            .Where(a => a.CharacterId == query.CharacterId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(a => new CharacterAuditEntryDto(
                a.Id, a.CreatedAt, a.Action, a.Summary, a.XpDelta, a.TotalXpAfter, a.SpentXpAfter))
            .ToListAsync(ct);
    }
}
