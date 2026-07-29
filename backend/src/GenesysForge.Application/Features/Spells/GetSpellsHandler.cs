using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Spells;

public class GetSpellsHandler(IAppDbContext db) : IQueryHandler<GetSpellsQuery, List<SpellDto>>
{
    public async Task<List<SpellDto>> Handle(GetSpellsQuery query, CancellationToken ct = default)
    {
        var rows = await db.SpellDefs.AsNoTracking()
            .Where(s => s.System == query.System && (s.OwnerUserId == null || s.OwnerUserId == query.UserId))
            .OrderBy(s => s.MagicSkill).ThenBy(s => s.Kind).ThenBy(s => s.ParentEffect)
            .ThenBy(s => s.SortOrder).ThenBy(s => s.NameRu)
            .ToListAsync(ct);

        // Списки доступности хранятся строкой, а отдаются массивом: разделитель — дело сервера,
        // клиенту достаётся готовый список направлений (ROT-MAG-01).
        return [.. rows.Select(s => new SpellDto(s.Id, s.MagicSkill, s.Kind, s.ParentEffect, s.NameRu,
            s.NameEn, s.Difficulty, s.Description, s.SafeDescription, s.Source, s.OwnerUserId != null,
            s.DescriptionEn, s.RestrictedSkill, s.Repeatable, Csv(s.AllowedSkills), s.DifficultyIncrease,
            Csv(s.Exclusions), s.Resolution, s.IsOptional))];
    }

    private static IReadOnlyList<string> Csv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
