using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Справочные таблицы правил (сложности, траты символов, дистанции, крит-ранения) из embedded JSON
/// (<c>SeedContent/rules.catalog.json</c>). Источник — RU-парафразы механики (не текст книг),
/// собран генератором <c>_books/gen-rules-catalog.mjs</c>. Таблицы системо-независимы.
/// </summary>
public static class RuleCatalog
{
    private sealed record Entry(
        string Kind, string Code, string NameRu, string NameEn, string GroupRu, int SortOrder,
        string RollRange, string SymbolCost, string Body, string Notes, string Source, string SourcePage,
        string GroupEn = "", string BodyEn = "", string NotesEn = "");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<RuleTableEntry> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(RuleCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("rules.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            if (!Enum.TryParse<RuleTableKind>(e.Kind, ignoreCase: true, out var kind)) continue;
            yield return new RuleTableEntry
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                Code = e.Code,
                NameRu = e.NameRu,
                NameEn = e.NameEn,
                GroupRu = e.GroupRu,
                GroupEn = e.GroupEn,
                SortOrder = e.SortOrder,
                RollRange = e.RollRange,
                SymbolCost = e.SymbolCost,
                Body = e.Body,
                BodyEn = e.BodyEn,
                Notes = e.Notes,
                NotesEn = e.NotesEn,
                Source = e.Source,
                SourcePage = e.SourcePage,
                SearchText = BuildSearchText(e),
            };
        }
    }

    /// <summary>Денормализованная lowercase-строка для серверного поиска по таблицам.</summary>
    private static string BuildSearchText(Entry e) =>
        string.Join(' ', new[] { e.NameRu, e.NameEn, e.GroupRu, e.GroupEn, e.SymbolCost, e.Body, e.BodyEn, e.Notes, e.NotesEn, e.RollRange }
            .Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();
}
