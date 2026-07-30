using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог покупаемых скакунов (ROT-MOUNT-ITEM-01) из embedded JSON
/// (<c>SeedContent/mounts.catalog.json</c>). Скакун — существо со статблоком, а не запись
/// снаряжения: раньше эти четыре профиля лежали в каталоге предметов с <c>Enc 0</c> и описанием
/// «Снаряжение», поэтому у купленного скакуна не было ни характеристик, ни ран, ни вместимости.
/// </summary>
public static class MountCatalog
{
    private sealed record Entry(
        string Code, string Name, string NameRu, string Setting, string Kind,
        int Brawn, int Agility, int Intellect, int Cunning, int Willpower, int Presence,
        int Soak, int WoundThreshold, int? StrainThreshold,
        int MeleeDefense, int RangedDefense, int Silhouette, int Capacity,
        int? Price, int Rarity,
        string[]? IncludedGear, bool RequiresRidingCheck,
        SkillEntry[]? Skills, AbilityEntry[]? Abilities, AttackEntry[]? Attacks,
        string Desc, string DescEn, string Source, bool Retired = false);

    /// <param name="Group">Групповой навык Minion: ранг даёт группа, а не запись.</param>
    private sealed record SkillEntry(string Name, int Ranks = 0, bool Group = false);

    private sealed record AbilityEntry(string Name, string NameRu = "", string Desc = "", string DescEn = "");

    private sealed record AttackEntry(
        string Name, string NameRu, string Skill, int Damage, int Critical,
        string Range = "Engaged", string[]? Qualities = null);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Стабильные bare-коды четырёх скакунов книги: по ним же выводятся legacy-предметы.</summary>
    public static readonly string[] BuiltInCodes =
        ["beast-of-burden", "riding-beast", "war-mount", "flying-mount"];

    /// <summary>Разворачивает каталог в список встроенных профилей по системам.</summary>
    public static IEnumerable<MountDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(MountCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("mounts.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            // Fantasy → только Realms of Terrinoth, иначе обе системы. То же правило, что у
            // предметов и улучшений.
            var systems = string.Equals(e.Setting, "Fantasy", StringComparison.OrdinalIgnoreCase)
                ? new[] { GameSystem.RealmsOfTerrinoth }
                : [GameSystem.GenesysCore, GameSystem.RealmsOfTerrinoth];

            foreach (var sys in systems)
                yield return new MountDef
                {
                    Id = Guid.NewGuid(),
                    System = sys,
                    Code = $"{(sys == GameSystem.GenesysCore ? "gc" : "rot")}.mount.{e.Code}",
                    Name = e.Name,
                    NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                    Kind = Enum.Parse<NpcKind>(e.Kind, ignoreCase: true),
                    Brawn = e.Brawn,
                    Agility = e.Agility,
                    Intellect = e.Intellect,
                    Cunning = e.Cunning,
                    Willpower = e.Willpower,
                    Presence = e.Presence,
                    Soak = e.Soak,
                    WoundThreshold = e.WoundThreshold,
                    StrainThreshold = e.StrainThreshold,
                    MeleeDefense = e.MeleeDefense,
                    RangedDefense = e.RangedDefense,
                    Silhouette = e.Silhouette,
                    Capacity = e.Capacity,
                    Price = e.Price,
                    Rarity = e.Rarity,
                    IncludedGear = [.. e.IncludedGear ?? []],
                    RequiresRidingCheck = e.RequiresRidingCheck,
                    Skills =
                    [
                        .. (e.Skills ?? []).Select(s => new MountSkill
                        {
                            Id = Guid.NewGuid(),
                            Name = s.Name,
                            Ranks = s.Ranks,
                            IsGroupSkill = s.Group,
                        }),
                    ],
                    Abilities =
                    [
                        .. (e.Abilities ?? []).Select(a => new MountAbility
                        {
                            Id = Guid.NewGuid(),
                            Name = a.Name,
                            NameRu = string.IsNullOrWhiteSpace(a.NameRu) ? a.Name : a.NameRu,
                            Description = a.Desc,
                            DescriptionEn = a.DescEn,
                        }),
                    ],
                    Attacks =
                    [
                        .. (e.Attacks ?? []).Select(a => new MountAttack
                        {
                            Id = Guid.NewGuid(),
                            Name = a.Name,
                            NameRu = string.IsNullOrWhiteSpace(a.NameRu) ? a.Name : a.NameRu,
                            SkillName = a.Skill,
                            Damage = a.Damage,
                            Critical = a.Critical,
                            Range = Enum.Parse<WeaponRange>(a.Range, ignoreCase: true),
                            QualityCodes = [.. a.Qualities ?? []],
                        }),
                    ],
                    SafeDescription = e.Desc,
                    DescriptionEn = e.DescEn,
                    Source = e.Source,
                    Retired = e.Retired,
                };
        }
    }
}
