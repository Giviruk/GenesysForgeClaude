using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Reference;

public class GetReferenceHandler(IAppDbContext db) : IQueryHandler<GetReferenceQuery, ReferenceResponse>
{
    public async Task<ReferenceResponse> Handle(GetReferenceQuery query, CancellationToken ct = default)
    {
        var (userId, system) = (query.UserId, query.System);

        var archetypes = await db.ArchetypeDefs.AsNoTracking()
            .Where(a => a.System == system).OrderBy(a => a.Name)
            .Select(a => a.ToDto()).ToListAsync(ct);

        var careers = await db.CareerDefs.AsNoTracking()
            .Where(c => c.System == system).OrderBy(c => c.Name)
            .Select(c => c.ToDto()).ToListAsync(ct);

        var skills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .OrderBy(s => s.Kind).ThenBy(s => s.Name)
            .Select(s => s.ToDto()).ToListAsync(ct);

        var talents = await db.TalentDefs.AsNoTracking()
            .Where(t => t.System == system && (t.OwnerUserId == null || t.OwnerUserId == userId))
            .OrderBy(t => t.Tier).ThenBy(t => t.Name)
            .Select(t => t.ToDto()).ToListAsync(ct);

        var items = await db.ItemDefs.AsNoTracking()
            .Where(i => i.System == system && (i.OwnerUserId == null || i.OwnerUserId == userId))
            .OrderBy(i => i.Kind).ThenBy(i => i.Name)
            .Select(i => i.ToDto()).ToListAsync(ct);

        var heroics = system == GameSystem.RealmsOfTerrinoth
            ? await db.HeroicAbilityDefs.AsNoTracking()
                .Where(h => h.OwnerUserId == null || h.OwnerUserId == userId)
                .OrderBy(h => h.Name)
                .Select(h => h.ToDto()).ToListAsync(ct)
            : [];

        return new ReferenceResponse(archetypes, careers, skills, talents, items, heroics);
    }
}
