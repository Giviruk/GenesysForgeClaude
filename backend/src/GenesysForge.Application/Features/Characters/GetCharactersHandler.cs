using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class GetCharactersHandler(IAppDbContext db) : IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>>
{
    public Task<List<CharacterListItemDto>> Handle(GetCharactersQuery query, CancellationToken ct = default) =>
        db.Characters.AsNoTracking()
            .Where(c => c.OwnerUserId == query.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CharacterListItemDto(c.Id, c.Name, c.System, c.Archetype!.NameRu, c.Career!.NameRu,
                c.IsCreationPhase, c.CreatedAt))
            .ToListAsync(ct);
}
