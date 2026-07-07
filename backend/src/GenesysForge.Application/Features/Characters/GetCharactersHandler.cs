using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
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
            var derived = SheetCalculator.ComputeDerived(
                c.Characteristics,
                c.Archetype!.WoundBase,
                c.Archetype.StrainBase,
                c.Talents.Select(t => new TalentInput(
                    t.TalentDef!.Name,
                    t.TalentDef.Tier,
                    t.Ranks,
                    t.TalentDef.WoundBonus,
                    t.TalentDef.StrainBonus,
                    t.TalentDef.SoakBonus,
                    t.TalentDef.MeleeDefenseBonus,
                    t.TalentDef.RangedDefenseBonus)).ToList(),
                c.Items.Select(i => new ItemInput(
                    i.ItemDef!.Name,
                    i.ItemDef.Kind,
                    i.State,
                    i.ItemDef.Encumbrance,
                    i.Quantity,
                    i.ItemDef.SoakBonus,
                    i.ItemDef.MeleeDefense,
                    i.ItemDef.RangedDefense,
                    i.ItemDef.EncumbranceThresholdBonus)).ToList());

            return new CharacterListItemDto(
                c.Id,
                c.Name,
                c.System,
                c.Archetype.NameRu,
                c.Career!.NameRu,
                c.IsCreationPhase,
                c.CreatedAt,
                c.AvailableXp,
                c.WoundsCurrent,
                derived.WoundThreshold,
                c.StrainCurrent,
                derived.StrainThreshold);
        }).ToList();
    }
}
