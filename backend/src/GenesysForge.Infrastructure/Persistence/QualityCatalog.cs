using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Справочник качеств предметов/заклинаний из embedded JSON (<c>SeedContent/qualities.catalog.json</c>).
/// Источник — пользовательский CSV (структура + переработанные RU-описания, не текст книг),
/// собран скриптом <c>_books/_qualities/gen-qualities-catalog.mjs</c>. Качества системо-независимы.
/// </summary>
public static class QualityCatalog
{
    private sealed record Entry(
        string Code, string NameEn, string NameRu, bool Active, bool HasRating,
        string ActivationCost, string Category, string Desc, string Safe, string Source);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<QualityDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(QualityCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("qualities.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
            yield return new QualityDef
            {
                Id = Guid.NewGuid(),
                Code = e.Code,
                NameEn = e.NameEn,
                NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.NameEn : e.NameRu,
                Kind = QualityKind.ItemQuality,
                IsActive = e.Active,
                HasRating = e.HasRating,
                ActivationCost = e.ActivationCost,
                Category = e.Category,
                Description = e.Desc,
                SafeDescription = e.Safe,
                Source = e.Source,
            };
    }
}
