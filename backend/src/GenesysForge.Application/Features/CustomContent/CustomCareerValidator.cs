using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

internal static class CustomCareerValidator
{
    public static async Task<List<string>> ValidateAndNormalizeSkillsAsync(
        IAppDbContext db, Guid userId, CreateCustomCareerRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название карьеры не может быть пустым.");
        if (req.StartingMoneyFixed < 0)
            throw new DomainRuleException("Стартовые деньги не могут быть отрицательными.");
        if (!string.IsNullOrWhiteSpace(req.StartingMoneyDice) && !System.Text.RegularExpressions.Regex.IsMatch(req.StartingMoneyDice.Trim(), @"^\d+d\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new DomainRuleException("Бросок стартовых денег должен быть в формате NdM, например 1d100.");

        var requested = (req.CareerSkillNames ?? [])
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count is < 1 or > 12)
            throw new DomainRuleException("Карьера должна содержать от 1 до 12 карьерных навыков.");

        var visibleSkills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == req.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .Select(s => s.Name)
            .ToListAsync(ct);
        var canonical = visibleSkills
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var normalized = new List<string>();
        foreach (var name in requested)
        {
            if (!canonical.TryGetValue(name, out var skillName))
                throw new DomainRuleException($"Навык «{name}» не найден в системе.");
            normalized.Add(skillName);
        }

        return normalized;
    }
}
