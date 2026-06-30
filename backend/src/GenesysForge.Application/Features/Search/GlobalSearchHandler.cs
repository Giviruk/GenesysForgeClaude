using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Search;

/// <summary>
/// Глобальный поиск по подстроке (регистронезависимо) в нескольких источниках. Каждый источник
/// ограничен <see cref="PerSource"/> совпадениями, чтобы ответ оставался компактным.
/// </summary>
public class GlobalSearchHandler(IAppDbContext db) : IQueryHandler<GlobalSearchQuery, SearchResponse>
{
    private const int PerSource = 10;

    private static string Snippet(string s) =>
        string.IsNullOrWhiteSpace(s) ? "" : (s.Length > 200 ? s[..197].TrimEnd() + "…" : s);

    public async Task<SearchResponse> Handle(GlobalSearchQuery query, CancellationToken ct = default)
    {
        var needle = (query.Query ?? "").Trim().ToLowerInvariant();
        var hits = new List<SearchHitDto>();
        if (needle.Length < 2) return new SearchResponse(hits);

        var system = query.System;
        var userId = query.UserId;

        // Системо-зависимая видимость контента: встроенный (OwnerUserId == null) или свой кастомный.
        var settingMask = system == GameSystem.RealmsOfTerrinoth
            ? GenesysSetting.Any | GenesysSetting.Fantasy
            : GenesysSetting.Any;

        // 1. Справочные таблицы правил (системо-независимы) — по денормализованному SearchText.
        hits.AddRange(await db.RuleTableEntries.AsNoTracking()
            .Where(r => r.SearchText.Contains(needle))
            .OrderBy(r => r.Kind).ThenBy(r => r.SortOrder).Take(PerSource)
            .Select(r => new SearchHitDto("rule", "Правила", r.NameRu, r.GroupRu, r.Body, "/reference"))
            .ToListAsync(ct));

        // 2. Навыки
        hits.AddRange(await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId)
                && (s.NameRu.ToLower().Contains(needle) || s.Name.ToLower().Contains(needle)))
            .OrderBy(s => s.NameRu).Take(PerSource)
            .Select(s => new SearchHitDto("skill", "Навыки", s.NameRu, s.Name, s.SafeDescription, "/reference"))
            .ToListAsync(ct));

        // 3. Таланты
        hits.AddRange(await db.TalentDefs.AsNoTracking()
            .Where(t => t.System == system
                && (t.OwnerUserId == userId || (t.OwnerUserId == null && (t.Setting & settingMask) != 0))
                && (t.NameRu.ToLower().Contains(needle) || t.Name.ToLower().Contains(needle)))
            .OrderBy(t => t.NameRu).Take(PerSource)
            .Select(t => new SearchHitDto("talent", "Таланты", t.NameRu, t.Name, t.SafeDescription, "/reference"))
            .ToListAsync(ct));

        // 4. Предметы
        hits.AddRange(await db.ItemDefs.AsNoTracking()
            .Where(i => i.System == system && (i.OwnerUserId == null || i.OwnerUserId == userId)
                && (i.NameRu.ToLower().Contains(needle) || i.Name.ToLower().Contains(needle)))
            .OrderBy(i => i.NameRu).Take(PerSource)
            .Select(i => new SearchHitDto("item", "Предметы", i.NameRu, i.Name, i.SafeDescription, "/reference"))
            .ToListAsync(ct));

        // 5. Качества (системо-независимы)
        hits.AddRange(await db.QualityDefs.AsNoTracking()
            .Where(q => q.NameRu.ToLower().Contains(needle) || q.NameEn.ToLower().Contains(needle))
            .OrderBy(q => q.NameRu).Take(PerSource)
            .Select(q => new SearchHitDto("quality", "Качества", q.NameRu, q.NameEn, q.SafeDescription, "/reference"))
            .ToListAsync(ct));

        // 6. Архетипы / виды
        hits.AddRange(await db.ArchetypeDefs.AsNoTracking()
            .Where(a => a.System == system && !a.Retired && (a.OwnerUserId == null || a.OwnerUserId == userId)
                && (a.NameRu.ToLower().Contains(needle) || a.Name.ToLower().Contains(needle)))
            .OrderBy(a => a.NameRu).Take(PerSource)
            .Select(a => new SearchHitDto("archetype", "Архетипы", a.NameRu, a.Name, a.SafeDescription, "/reference"))
            .ToListAsync(ct));

        // 7. Карьеры
        hits.AddRange(await db.CareerDefs.AsNoTracking()
            .Where(c => c.System == system && (c.OwnerUserId == null || c.OwnerUserId == userId)
                && (c.NameRu.ToLower().Contains(needle) || c.Name.ToLower().Contains(needle)))
            .OrderBy(c => c.NameRu).Take(PerSource)
            .Select(c => new SearchHitDto("career", "Карьеры", c.NameRu, c.Name, c.SafeDescription, "/reference"))
            .ToListAsync(ct));

        // 8. Героика (только Realms of Terrinoth)
        if (system == GameSystem.RealmsOfTerrinoth)
            hits.AddRange(await db.HeroicAbilityDefs.AsNoTracking()
                .Where(h => (h.OwnerUserId == null || h.OwnerUserId == userId)
                    && (h.NameRu.ToLower().Contains(needle) || h.Name.ToLower().Contains(needle)))
                .OrderBy(h => h.NameRu).Take(PerSource)
                .Select(h => new SearchHitDto("heroic", "Героика", h.NameRu, h.Name, h.SafeDescription, "/reference"))
                .ToListAsync(ct));

        // 9. NPC пользователя
        hits.AddRange(await db.Npcs.AsNoTracking()
            .Where(n => n.OwnerUserId == userId
                && (n.Name.ToLower().Contains(needle) || n.Description.ToLower().Contains(needle)))
            .OrderBy(n => n.Name).Take(PerSource)
            .Select(n => new SearchHitDto("npc", "NPC", n.Name, n.Source, n.Description, "/npcs/" + n.Id))
            .ToListAsync(ct));

        // 10. Персонажи пользователя
        hits.AddRange(await db.Characters.AsNoTracking()
            .Where(c => c.OwnerUserId == userId && c.Name.ToLower().Contains(needle))
            .OrderBy(c => c.Name).Take(PerSource)
            .Select(c => new SearchHitDto("character", "Персонажи", c.Name, "", "", "/characters/" + c.Id))
            .ToListAsync(ct));

        // Усечение длинных описаний — в памяти (Snippet не транслируется в SQL).
        return new SearchResponse(hits.Select(h => h with { Snippet = Snippet(h.Snippet) }).ToList());
    }
}
