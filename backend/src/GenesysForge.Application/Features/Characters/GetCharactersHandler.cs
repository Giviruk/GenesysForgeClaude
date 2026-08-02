using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class GetCharactersHandler(IAppDbContext db) : IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>>
{
    public async Task<List<CharacterListItemDto>> Handle(GetCharactersQuery query, CancellationToken ct = default)
    {
        var characters = await db.CardListQuery(query.UserId).ToListAsync(ct);

        return characters.Select(c =>
        {
            var thresholds = CharacterDerived.Thresholds(c);

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
                thresholds.Wound,
                c.StrainCurrent,
                thresholds.Strain,
                c.PortraitUrl);
        }).ToList();
    }
}
