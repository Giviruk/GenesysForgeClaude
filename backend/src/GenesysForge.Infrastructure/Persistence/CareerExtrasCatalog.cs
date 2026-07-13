using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог «extras» карьер из embedded JSON (<c>SeedContent/career-extras.catalog.json</c>):
/// стартовые деньги, стартовое снаряжение (с выборами) и правила/заметки. Источник — пользовательский
/// CSV (структура + переработанные RU-описания, не текст книг), собран <c>_books/gen-career-extras-catalog.mjs</c>.
/// Ключ — <see cref="CareerDef.Code"/>; сид раскладывает данные по существующим карьерам.
/// </summary>
public static class CareerExtrasCatalog
{
    public sealed record Extras(string Code, int MoneyFixed, string MoneyDice,
        List<CareerStartingGear> Gear, List<CareerRule> Rules);

    private sealed record Entry(string Code, int MoneyFixed, string MoneyDice,
        List<GearEntry>? Gear, List<RuleEntry>? Rules);

    private sealed record GearEntry(string ItemCode, string ItemNameFallback, int Quantity,
        bool IsChoice, string ChoiceGroup, int ChoiceOption);

    private sealed record RuleEntry(string Code, string Kind, string Description, string DescriptionEn = "");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEnumerable<Extras> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(CareerExtrasCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("career-extras.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            var gear = (e.Gear ?? []).Select(g => new CareerStartingGear
            {
                Id = Guid.NewGuid(), ItemCode = g.ItemCode, ItemNameFallback = g.ItemNameFallback,
                Quantity = g.Quantity, IsChoice = g.IsChoice, ChoiceGroup = g.ChoiceGroup, ChoiceOption = g.ChoiceOption,
            }).ToList();

            var rules = (e.Rules ?? []).Select(r => new CareerRule
            {
                Id = Guid.NewGuid(), Code = r.Code, Description = r.Description, DescriptionEn = r.DescriptionEn,
                Kind = Enum.TryParse<CareerRuleKind>(r.Kind, ignoreCase: true, out var k) ? k : CareerRuleKind.Advisory,
            }).ToList();

            yield return new Extras(e.Code, e.MoneyFixed, e.MoneyDice, gear, rules);
        }
    }
}
