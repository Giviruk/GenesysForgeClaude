using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог улучшений (ROT-EQP-ATT-01…03), загружаемый из embedded JSON
/// (<c>SeedContent/attachments.catalog.json</c>). Улучшения — собственный тип контента: раньше они
/// лежали в каталоге предметов обычным снаряжением, поэтому их можно было купить, но не поставить.
/// </summary>
public static class AttachmentCatalog
{
    private sealed record Entry(
        string Code, string Name, string NameRu, string Setting,
        int HardPointCost, int? Price, int Rarity, bool IsEnchantment,
        string HostKind,
        string[]? RequiredTraits, string[]? RequiredAnyTraits, string[]? ForbiddenTraits,
        string Desc, string DescEn, string Source,
        EffectEntry[]? Effects);

    /// <param name="Kind">Имя <see cref="AttachmentEffectKind"/>.</param>
    /// <param name="Condition">Имя <see cref="AttachmentEffectCondition"/>; пусто — всегда.</param>
    private sealed record EffectEntry(
        string Kind, string? Quality = null, string? Opposite = null, string? Skill = null,
        int Value = 0, int Increment = 0, string? Condition = null, string Note = "");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Разворачивает каталог в список встроенных улучшений по системам.</summary>
    public static IEnumerable<AttachmentDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(AttachmentCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("attachments.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            // Core-compatible attachments проходят собственный утверждённый каталог: для них Any
            // по-прежнему означает обе системы. ItemDef использует отдельный allowlist includeInRot.
            var systems = string.Equals(e.Setting, "Fantasy", StringComparison.OrdinalIgnoreCase)
                ? new[] { GameSystem.RealmsOfTerrinoth }
                : [GameSystem.GenesysCore, GameSystem.RealmsOfTerrinoth];

            foreach (var sys in systems)
                yield return new AttachmentDef
                {
                    Id = Guid.NewGuid(),
                    System = sys,
                    Code = $"{(sys == GameSystem.GenesysCore ? "gc" : "rot")}.attachment.{e.Code}",
                    Name = e.Name,
                    NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                    HardPointCost = e.HardPointCost,
                    Price = e.Price,
                    Rarity = e.Rarity,
                    IsEnchantment = e.IsEnchantment,
                    HostKind = ParseHostKind(e.HostKind),
                    RequiredTraits = ItemCatalog.ParseTraits(e.RequiredTraits),
                    RequiredAnyTraits = ItemCatalog.ParseTraits(e.RequiredAnyTraits),
                    ForbiddenTraits = ItemCatalog.ParseTraits(e.ForbiddenTraits),
                    SafeDescription = e.Desc,
                    DescriptionEn = e.DescEn,
                    Source = e.Source,
                    Effects = [.. (e.Effects ?? []).Select(x => new AttachmentEffect
                    {
                        Id = Guid.NewGuid(),
                        Kind = Enum.Parse<AttachmentEffectKind>(x.Kind, ignoreCase: true),
                        QualityCode = x.Quality ?? "",
                        OppositeQualityCode = x.Opposite ?? "",
                        SkillName = x.Skill ?? "",
                        Value = x.Value,
                        Increment = x.Increment,
                        Condition = string.IsNullOrEmpty(x.Condition)
                            ? AttachmentEffectCondition.Always
                            : Enum.Parse<AttachmentEffectCondition>(x.Condition, ignoreCase: true),
                        Note = x.Note,
                    })],
                };
        }
    }

    private static ItemKind ParseHostKind(string kind) => kind.ToLowerInvariant() switch
    {
        "weapon" => ItemKind.Weapon,
        "armor" => ItemKind.Armor,
        _ => throw new InvalidOperationException($"Улучшение не ставится на «{kind}»."),
    };
}
