using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог снаряжения, загружаемый из embedded JSON (<c>SeedContent/items.catalog.json</c>).
/// Источник — пользовательский CSV (структура + переработанные RU-описания, не текст книг),
/// собран скриптом <c>_books/_inventory/gen-items-catalog.mjs</c>.
/// Каждая запись разворачивается в <see cref="ItemDef"/> по игровым системам согласно сеттингу:
/// Any → Genesys Core и Realms of Terrinoth; Fantasy → только Realms of Terrinoth.
/// </summary>
public static class ItemCatalog
{
    private sealed record Entry(
        string Code, string Name, string NameRu, string Kind, string Setting,
        int Enc, int Soak, int Def, int Rdef, int EncBonus, int Price, int Rarity,
        string Desc, string Source,
        string? SkillEn, string? Damage, string? Crit, string? RangeRu, string? Properties);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Разворачивает каталог в список встроенных предметов по системам.</summary>
    public static IEnumerable<ItemDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(ItemCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("items.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            // Any → обе системы; Fantasy → только Realms of Terrinoth.
            var systems = string.Equals(e.Setting, "Fantasy", StringComparison.OrdinalIgnoreCase)
                ? new[] { GameSystem.RealmsOfTerrinoth }
                : [GameSystem.GenesysCore, GameSystem.RealmsOfTerrinoth];

            var kind = ParseKind(e.Kind);

            foreach (var sys in systems)
                yield return new ItemDef
                {
                    Id = Guid.NewGuid(),
                    System = sys,
                    Code = $"{(sys == GameSystem.GenesysCore ? "gc" : "rot")}.item.{e.Code}",
                    Name = e.Name,
                    NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                    Kind = kind,
                    Encumbrance = e.Enc,
                    SoakBonus = e.Soak,
                    MeleeDefense = e.Def,
                    RangedDefense = e.Rdef,
                    EncumbranceThresholdBonus = e.EncBonus,
                    Price = e.Price,
                    Rarity = e.Rarity,
                    SafeDescription = e.Desc,
                    Source = e.Source,
                    SkillName = e.SkillEn ?? "",
                    Damage = e.Damage ?? "",
                    Crit = e.Crit ?? "",
                    RangeBand = e.RangeRu ?? "",
                    Properties = e.Properties ?? "",
                };
        }
    }

    private static ItemKind ParseKind(string kind) => kind.ToLowerInvariant() switch
    {
        "weapon" => ItemKind.Weapon,
        "armor" => ItemKind.Armor,
        _ => ItemKind.Gear,
    };
}
