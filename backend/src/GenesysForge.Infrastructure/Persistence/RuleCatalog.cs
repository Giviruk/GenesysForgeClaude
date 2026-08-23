using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Справочные таблицы правил из embedded JSON (<c>SeedContent/rules.catalog.json</c>).
/// Свойства оружия проецируются из общего каталога качеств, чтобы описание в справочнике
/// совпадало с подсказками предметов. Источник — RU-парафразы механики (не текст книг).
/// Таблицы системо-независимы.
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

        // Качества оружия уже используются предметами и тултипами. Добавляем их в справочник
        // из того же каталога, чтобы не поддерживать вторую копию описаний и не расходиться
        // с карточками снаряжения. Категории магических эффектов намеренно не попадают сюда:
        // они описываются в разделе магических действий.
        var weaponQualities = QualityCatalog.Load(assembly)
            .Where(q => q.Category.StartsWith("Оружие", StringComparison.OrdinalIgnoreCase))
            .OrderBy(q => q.NameRu, StringComparer.OrdinalIgnoreCase)
            .ToList();
        for (var i = 0; i < weaponQualities.Count; i++)
        {
            var q = weaponQualities[i];
            var notes = q.IsActive
                ? "Активное свойство: обычно требует оплаты преимуществами, указанной в строке активации."
                : "Пассивное свойство: действует постоянно, пока предмет используется по назначению.";
            if (q.HasRating) notes += " Рейтинг после названия меняет силу эффекта.";

            yield return new RuleTableEntry
            {
                Id = Guid.NewGuid(),
                Kind = RuleTableKind.WeaponProperty,
                Code = $"weapon-property-{q.Code}",
                NameRu = q.NameRu,
                NameEn = q.NameEn,
                GroupRu = q.Category,
                GroupEn = "Weapon",
                SortOrder = i,
                RollRange = "",
                SymbolCost = string.IsNullOrWhiteSpace(q.ActivationCost) ? "—" : q.ActivationCost,
                Body = q.Description,
                BodyEn = q.DescriptionEn,
                Notes = notes,
                NotesEn = q.IsActive
                    ? "Active quality: normally requires the Advantage cost shown in the activation field."
                    : "Passive quality: it applies continuously while the item is used appropriately.",
                Source = q.Source,
                SourcePage = "",
                SearchText = string.Join(' ', new[]
                {
                    q.NameRu, q.NameEn, q.Category, q.ActivationCost, q.Description,
                    q.DescriptionEn, notes, q.Source,
                }.Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant(),
            };
        }
    }

    /// <summary>Денормализованная lowercase-строка для серверного поиска по таблицам.</summary>
    private static string BuildSearchText(Entry e) =>
        string.Join(' ', new[] { e.NameRu, e.NameEn, e.GroupRu, e.GroupEn, e.SymbolCost, e.Body, e.BodyEn, e.Notes, e.NotesEn, e.RollRange }
            .Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();
}
