using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class GetCharactersHandler(IAppDbContext db) : IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>>
{
    public async Task<List<CharacterListItemDto>> Handle(GetCharactersQuery query, CancellationToken ct = default)
    {
        var characters = await db.Characters.AsNoTracking()
            .Where(c => c.OwnerUserId == query.UserId)
            .Include(c => c.Archetype)
            .Include(c => c.Career)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return characters.Select(c =>
        {
            var derived = CharacterDerived.Compute(c);

            return new CharacterListItemDto(
                c.Id,
                c.Name,
                c.System,
                c.Archetype!.NameRu,
                c.Career!.NameRu,
                c.IsCreationPhase,
                c.CreatedAt,
                c.AvailableXp,
                c.WoundsCurrent,
                derived.WoundThreshold,
                c.StrainCurrent,
                derived.StrainThreshold,
                c.PortraitUrl);
        }).ToList();
    }
}
