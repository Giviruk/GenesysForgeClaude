using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Reference;

public class GetRulesHandler(IAppDbContext db) : IQueryHandler<GetRulesQuery, RulesResponse>
{
    public async Task<RulesResponse> Handle(GetRulesQuery query, CancellationToken ct = default)
    {
        var q = db.RuleTableEntries.AsNoTracking();

        var needle = query.Query?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(needle))
            q = q.Where(r => r.SearchText.Contains(needle));

        var entries = await q
            .OrderBy(r => r.Kind).ThenBy(r => r.SortOrder)
            .Select(r => r.ToDto())
            .ToListAsync(ct);

        return new RulesResponse(entries);
    }
}
