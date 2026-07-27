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
        string? SkillEn, string? Damage, string? Crit, string? RangeRu, string? Properties,
        string DescEn = "", bool Retired = false,
        /// <summary>Слоты улучшений по таблице книги; null — книжного значения у записи нет.</summary>
        int? Hp = null,
        /// <summary>Влияние предмета на проверки навыков (ROT-ARM-01).</summary>
        ModifierEntry[]? Modifiers = null);

    /// <param name="Kind">«AddSetback» или «RemoveSetback».</param>
    /// <param name="Skill">Английское имя навыка; пусто — отбор по характеристике.</param>
    /// <param name="Characteristic">Имя характеристики; пусто — отбор по навыку.</param>
    /// <param name="Worn">Действует только когда предмет надет (для брони — выбран активной).</param>
    private sealed record ModifierEntry(
        string Kind, string? Skill, string? Characteristic, int Value, bool Worn = true, string Condition = "");

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
                    DescriptionEn = e.DescEn,
                    Source = e.Source,
                    SkillName = e.SkillEn ?? "",
                    Damage = e.Damage ?? "",
                    Crit = e.Crit ?? "",
                    RangeBand = e.RangeRu ?? "",
                    Properties = e.Properties ?? "",
                    Retired = e.Retired,
                    HardPoints = e.Hp,
                    CheckModifiers = [.. (e.Modifiers ?? []).Select(m => new ItemCheckModifier
                    {
                        Id = Guid.NewGuid(),
                        Kind = ParseModifierKind(m.Kind),
                        SkillName = m.Skill ?? "",
                        Characteristic = ParseCharacteristic(m.Characteristic),
                        Value = m.Value,
                        RequiresWorn = m.Worn,
                        Condition = m.Condition,
                    })],
                };
        }
    }

    private static ItemKind ParseKind(string kind) => kind.ToLowerInvariant() switch
    {
        "weapon" => ItemKind.Weapon,
        "armor" => ItemKind.Armor,
        _ => ItemKind.Gear,
    };

    private static CheckModifierKind ParseModifierKind(string kind) =>
        Enum.TryParse<CheckModifierKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Неизвестный вид модификатора проверки: «{kind}».");

    private static CharacteristicType? ParseCharacteristic(string? characteristic) =>
        string.IsNullOrWhiteSpace(characteristic)
            ? null
            : Enum.TryParse<CharacteristicType>(characteristic, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException($"Неизвестная характеристика: «{characteristic}».");
}
