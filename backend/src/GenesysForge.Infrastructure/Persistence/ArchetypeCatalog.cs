using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог архетипов/видов из embedded JSON (<c>SeedContent/archetypes.catalog.json</c>).
/// Источник — пользовательский CSV (структура + переработанные RU-описания, не текст книг),
/// собран генератором <c>_books/gen-archetypes-catalog.mjs</c>. Сеттинг «Any» → Genesys Core,
/// «Fantasy» → Realms of Terrinoth.
/// </summary>
public static class ArchetypeCatalog
{
    private sealed record Entry(
        string System, string Code, string Name, string NameRu,
        int Brawn, int Agility, int Intellect, int Cunning, int Willpower, int Presence,
        int WoundBase, int StrainBase, int StartingXp, string Safe, string Source);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<ArchetypeDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(ArchetypeCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("archetypes.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            if (!Enum.TryParse<GameSystem>(e.System, ignoreCase: true, out var system)) continue;
            yield return new ArchetypeDef
            {
                Id = Guid.NewGuid(), System = system, Code = e.Code,
                Name = e.Name, NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                Brawn = e.Brawn, Agility = e.Agility, Intellect = e.Intellect,
                Cunning = e.Cunning, Willpower = e.Willpower, Presence = e.Presence,
                WoundBase = e.WoundBase, StrainBase = e.StrainBase, StartingXp = e.StartingXp,
                SafeDescription = e.Safe, Source = e.Source,
            };
        }
    }
}
