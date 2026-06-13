using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Spells;

public class GetSpellsHandler(IAppDbContext db) : IQueryHandler<GetSpellsQuery, List<SpellDto>>
{
    public Task<List<SpellDto>> Handle(GetSpellsQuery query, CancellationToken ct = default) =>
        db.SpellDefs.AsNoTracking()
            .Where(s => s.System == query.System && (s.OwnerUserId == null || s.OwnerUserId == query.UserId))
            .OrderBy(s => s.MagicSkill).ThenBy(s => s.Kind).ThenBy(s => s.SortOrder).ThenBy(s => s.NameRu)
            .Select(s => new SpellDto(s.Id, s.MagicSkill, s.Kind, s.NameRu, s.NameEn,
                s.Difficulty, s.Description, s.OwnerUserId != null))
            .ToListAsync(ct);
}
