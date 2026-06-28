using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Встроенный бестиарий из embedded JSON (<c>SeedContent/bestiary.catalog.json</c>): официальные
/// существа Realms of Terrinoth (статблоки + короткие механические описания, не текст книги).
/// Источник — пользовательский датасет противников, разложен <c>_books/gen-bestiary-catalog.mjs</c>.
/// Каждое существо — <see cref="Npc"/> с <c>IsBuiltIn = true</c> и <c>OwnerUserId = null</c>:
/// read-only, видно всем, клонируется в свою библиотеку (см. NpcMapper.CanViewAsync / DuplicateNpc).
/// </summary>
public static class BestiaryCatalog
{
    private sealed record Entry(
        string System, string Name, string NameEn, string Kind, string Role, string Source, string? Description,
        int Brawn, int Agility, int Intellect, int Cunning, int Willpower, int Presence,
        int WoundThreshold, int? StrainThreshold, int Soak, int MeleeDefense, int RangedDefense, int Silhouette,
        string? Tactics,
        List<SkillEntry>? Skills, List<AbilityEntry>? Abilities, List<AttackEntry>? Attacks,
        List<string>? Talents, List<string>? Equipment, List<string>? Tags);

    private sealed record SkillEntry(string Name, int Ranks);
    private sealed record AbilityEntry(string Name, string? Description);
    private sealed record AttackEntry(string Name, string Skill, string Damage, string? Critical,
        string Range, string? Notes, List<QualityEntry>? Qualities);
    private sealed record QualityEntry(string Code, string? NameRu, int? Rating);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Загружает встроенных существ из каталога. Возвращает новые сущности (IsBuiltIn, owner=null).</summary>
    public static List<Npc> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(BestiaryCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("bestiary.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) return [];

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        return entries.Select(ToNpc).ToList();
    }

    private static Npc ToNpc(Entry e)
    {
        var kind = Enum.TryParse<NpcKind>(e.Kind, ignoreCase: true, out var k) ? k : NpcKind.Rival;
        var npc = new Npc
        {
            Id = Guid.NewGuid(),
            OwnerUserId = null,
            IsBuiltIn = true,
            System = Enum.TryParse<GameSystem>(e.System, ignoreCase: true, out var sys) ? sys : GameSystem.RealmsOfTerrinoth,
            Name = e.Name.Trim(),
            Kind = kind,
            Role = Enum.TryParse<NpcRole>(e.Role, ignoreCase: true, out var role) ? role : NpcRole.Custom,
            Description = e.Description?.Trim() ?? "",
            Source = e.Source.Trim(),
            Brawn = e.Brawn, Agility = e.Agility, Intellect = e.Intellect,
            Cunning = e.Cunning, Willpower = e.Willpower, Presence = e.Presence,
            WoundThreshold = e.WoundThreshold,
            // Миньон не имеет порога усталости (групповые правила).
            StrainThreshold = kind == NpcKind.Minion ? null : e.StrainThreshold,
            Soak = e.Soak,
            MeleeDefense = e.MeleeDefense,
            RangedDefense = e.RangedDefense,
            Silhouette = e.Silhouette,
            Tactics = e.Tactics?.Trim() ?? "",
            Visibility = NpcVisibility.PublicTemplate,
            Talents = Clean(e.Talents),
            Equipment = Clean(e.Equipment),
            Tags = Clean(e.Tags),
        };

        // Миньон использует групповые навыки — индивидуальные ранги не значимы (как NpcMapper.Apply).
        var minion = kind == NpcKind.Minion;
        npc.Skills = (e.Skills ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => new NpcSkill { NpcId = npc.Id, Name = s.Name.Trim(), Ranks = minion ? 0 : s.Ranks })
            .ToList();
        npc.Abilities = (e.Abilities ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new NpcAbility { NpcId = npc.Id, Name = a.Name.Trim(), Description = a.Description?.Trim() ?? "" })
            .ToList();
        npc.Attacks = (e.Attacks ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new NpcAttack
            {
                NpcId = npc.Id,
                Name = a.Name.Trim(),
                SkillName = a.Skill?.Trim() ?? "",
                Damage = a.Damage?.Trim() ?? "",
                Critical = a.Critical?.Trim() ?? "",
                RangeBand = a.Range?.Trim() ?? "",
                Notes = a.Notes?.Trim() ?? "",
                Qualities = (a.Qualities ?? [])
                    .Where(q => !string.IsNullOrWhiteSpace(q.Code) || !string.IsNullOrWhiteSpace(q.NameRu))
                    .Select(q => new NpcAttackQuality
                    {
                        QualityCode = q.Code?.Trim() ?? "",
                        NameRu = q.NameRu?.Trim() ?? "",
                        Rating = q.Rating,
                    }).ToList(),
            }).ToList();

        return npc;
    }

    private static List<string> Clean(List<string>? values) =>
        (values ?? []).Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
}
