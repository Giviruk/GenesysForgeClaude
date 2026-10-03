using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Notes;

public class GetCharacterNotesHandler(IAppDbContext db) : IQueryHandler<GetCharacterNotesQuery, List<CharacterNoteDto>>
{
    public async Task<List<CharacterNoteDto>> Handle(GetCharacterNotesQuery query, CancellationToken ct = default)
    {
        // проверка владения персонажем (бросит, если не его)
        await db.EnsureOwnedAsync(query.UserId, query.CharacterId, ct);

        return await db.CharacterNotes.AsNoTracking()
            .Where(n => n.CharacterId == query.CharacterId && n.OwnerUserId == query.UserId)
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new CharacterNoteDto(n.Id, n.Title, n.Body, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(ct);
    }
}
