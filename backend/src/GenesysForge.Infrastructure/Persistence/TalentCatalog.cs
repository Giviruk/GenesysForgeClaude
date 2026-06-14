using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог талантов, загружаемый из embedded JSON (<c>SeedContent/talents.catalog.json</c>).
/// Источник — пользовательские CSV (структура + уже переработанные описания, не текст книг).
/// Каждая запись разворачивается в <see cref="TalentDef"/> по игровым системам согласно сеттингу:
/// Any → Genesys Core и Realms of Terrinoth; Fantasy → только Realms of Terrinoth.
/// </summary>
public static class TalentCatalog
{
    private sealed record Entry(
        string Code, string Name, string NameRu, int Tier, bool Ranked,
        string[] Setting, string Activation, string Desc,
        int Wt, int St, int Soak, int Mdef, int Rdef);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Разворачивает каталог в список встроенных талантов по системам.</summary>
    public static IEnumerable<TalentDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(TalentCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("talents.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            var setting = ParseSetting(e.Setting);
            // Any → обе системы; иначе (Fantasy/WeirdWar) → только Realms of Terrinoth.
            var systems = setting.HasFlag(GenesysSetting.Any)
                ? new[] { GameSystem.GenesysCore, GameSystem.RealmsOfTerrinoth }
                : [GameSystem.RealmsOfTerrinoth];

            var source = setting.HasFlag(GenesysSetting.Any)
                ? "Genesys: расширенный список талантов"
                : "Realms of Terrinoth, гл. «Таланты»";

            foreach (var sys in systems)
                yield return new TalentDef
                {
                    Id = Guid.NewGuid(),
                    System = sys,
                    Code = $"{(sys == GameSystem.GenesysCore ? "gc" : "rot")}.talent.{e.Code}",
                    Name = e.Name,
                    NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                    Tier = e.Tier,
                    IsRanked = e.Ranked,
                    Setting = setting,
                    Activation = e.Activation,
                    SafeDescription = e.Desc,
                    WoundBonus = e.Wt, StrainBonus = e.St, SoakBonus = e.Soak,
                    MeleeDefenseBonus = e.Mdef, RangedDefenseBonus = e.Rdef,
                    Source = source,
                };
        }
    }

    private static GenesysSetting ParseSetting(IEnumerable<string> flags)
    {
        var result = GenesysSetting.None;
        foreach (var f in flags)
            if (Enum.TryParse<GenesysSetting>(f, ignoreCase: true, out var v))
                result |= v;
        return result == GenesysSetting.None ? GenesysSetting.Any : result;
    }
}
