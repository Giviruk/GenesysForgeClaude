using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог героических способностей Realms of Terrinoth, загружаемый из embedded JSON
/// (<c>SeedContent/heroics.catalog.json</c>). Источник — пользовательский CSV
/// (структура книги + переработанные RU-описания, не текст книг), собран скриптом
/// <c>_books/_heroic_abilities/gen-heroics-catalog.mjs</c>.
/// Каждая запись несёт базовый эффект и два улучшения (Improved → 1 очко, Supreme → 2 очка).
/// </summary>
public static class HeroicCatalog
{
    private sealed record UpgradeEntry(string Level, int Cost, string Desc, string Notes);

    private sealed record Entry(
        string Code, string Name, string NameRu,
        string Requirement, string ActivationCost, string Activation, string Duration, string Frequency,
        string Desc, string Notes, string Source, string Page, List<UpgradeEntry> Upgrades);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Разворачивает каталог в список встроенных героических способностей (только Realms of Terrinoth).</summary>
    public static IEnumerable<HeroicAbilityDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(HeroicCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("heroics.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            var source = string.IsNullOrWhiteSpace(e.Page)
                ? "Realms of Terrinoth, гл. «Героические способности»"
                : $"Realms of Terrinoth, с. {e.Page}";

            yield return new HeroicAbilityDef
            {
                Id = Guid.NewGuid(),
                Code = $"rot.heroic.{e.Code}",
                Name = e.Name,
                NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                SafeDescription = e.Desc,
                Requirement = e.Requirement,
                ActivationCost = e.ActivationCost,
                Activation = e.Activation,
                Duration = e.Duration,
                Frequency = e.Frequency,
                Notes = e.Notes,
                Source = source,
                Upgrades = (e.Upgrades ?? []).Select(u => new HeroicAbilityUpgradeDef
                {
                    Id = Guid.NewGuid(),
                    Level = ParseLevel(u.Level),
                    Cost = u.Cost,
                    Description = u.Desc,
                    Notes = u.Notes,
                }).ToList(),
                Effects = EffectsFor(e.Code),
            };
        }
    }

    /// <summary>
    /// Структурная разметка эффектов автоматизации (U-18) по коду героики. Размечены только способности
    /// с явным механическим эффектом; остальные (нарратив/GM-решения) без эффектов → ручная подсказка.
    /// </summary>
    private static List<RuleEffectDef> EffectsFor(string code) => code switch
    {
        "hard-to-kill" =>
            [new RuleEffectDef { Id = Guid.NewGuid(), Kind = RuleEffectKind.AdjustSoak, Amount = 4,
                Duration = "пока способность активна", Description = "+4 к поглощению" }],
        "miraculous-recovery" =>
            [new RuleEffectDef { Id = Guid.NewGuid(), Kind = RuleEffectKind.HealWounds, Amount = 3,
                Description = "Лечит 3 раны" }],
        _ => [],
    };

    private static HeroicUpgradeLevel ParseLevel(string level) =>
        string.Equals(level, "Supreme", StringComparison.OrdinalIgnoreCase)
            ? HeroicUpgradeLevel.Supreme
            : HeroicUpgradeLevel.Improved;
}
