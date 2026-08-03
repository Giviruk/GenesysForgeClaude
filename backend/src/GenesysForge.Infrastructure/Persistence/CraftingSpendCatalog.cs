using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Таблицы трат символов изготовления и алхимии (ROT-CRAFT-01, ROT-ALCH-02) из embedded JSON
/// (<c>SeedContent/crafting.catalog.json</c>). Каждая строка книги предлагает несколько
/// взаимоисключающих эффектов за одну цену — в каталоге это отдельные записи с общим
/// <c>rowCode</c>, потому что выбирают и проверяют именно эффект, а не строку.
/// </summary>
public static class CraftingSpendCatalog
{
    private sealed record Entry(
        string Code, string RowCode, string Table,
        string NameRu, string Name, string Desc, string DescEn, string Safe, string Source,
        int AdvantageCost, int ThreatCost, int TriumphCost, int DespairCost,
        bool IsNegative, bool Repeatable, bool RequiresGmConfirmation, bool RequiresParameter,
        string Effect, int Value, string Quality, bool WeaponOnly, int SortOrder);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Разворачивает каталог в список встроенных трат.</summary>
    public static List<CraftingSpendDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(CraftingSpendCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("crafting.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) return [];

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<CraftingSpendDef>();
        foreach (var e in entries)
        {
            if (!seen.Add(e.Code))
                throw new InvalidOperationException($"Траты изготовления: дубль кода «{e.Code}».");
            // Трата, которую нечем оплатить, — сломанная строка каталога, а не бесплатный эффект.
            if (e.AdvantageCost + e.ThreatCost + e.TriumphCost + e.DespairCost == 0)
                throw new InvalidOperationException($"Трата «{e.Code}» без стоимости в символах.");
            result.Add(new CraftingSpendDef
            {
                Id = Guid.NewGuid(),
                Code = e.Code,
                RowCode = e.RowCode,
                Table = Enum.TryParse<CraftingKind>(e.Table, ignoreCase: true, out var table)
                    ? table : CraftingKind.Item,
                NameRu = e.NameRu,
                Name = e.Name,
                Description = e.Desc,
                SafeDescription = e.Safe,
                DescriptionEn = e.DescEn,
                Source = e.Source,
                AdvantageCost = e.AdvantageCost,
                ThreatCost = e.ThreatCost,
                TriumphCost = e.TriumphCost,
                DespairCost = e.DespairCost,
                IsNegative = e.IsNegative,
                Repeatable = e.Repeatable,
                RequiresGmConfirmation = e.RequiresGmConfirmation,
                RequiresParameter = e.RequiresParameter,
                Effect = Enum.TryParse<CraftingSpendEffect>(e.Effect, ignoreCase: true, out var effect)
                    ? effect : CraftingSpendEffect.Descriptive,
                Value = e.Value,
                Quality = e.Quality,
                WeaponOnly = e.WeaponOnly,
                SortOrder = e.SortOrder,
            });
        }
        return result;
    }
}
