using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Compatibility labels for renamed built-in skills; stored identifiers use English Name.</summary>
public static class NpcSkillNames
{
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Discipline"] = ["Выдержка", "Дисциплина"], ["Operating"] = ["Управление", "Операторство"],
        ["Perception"] = ["Внимание", "Внимательность", "Восприятие"], ["Resilience"] = ["Стойкость", "Выносливость"],
        ["Skulduggery"] = ["Жульничество", "Плутовство"], ["Streetwise"] = ["Уличная интуиция", "Уличная смекалка"],
        ["Brawl"] = ["Грубая сила", "Рукопашный бой"], ["Gunnery"] = ["Артиллерия", "Тяжёлое орудие"],
        ["Coercion"] = ["Запугивание", "Принуждение"],
        ["Ranged (Heavy)"] = ["Дальний бой (тяжёлое оружие)", "Дальний бой (тяжёлый)"],
        ["Ranged (Light)"] = ["Дальний бой (лёгкое оружие)", "Дальний бой (лёгкий)"],
        ["Melee (Heavy)"] = ["Ближний бой (тяжёлое оружие)", "Ближний бой (тяжёлый)"],
        ["Melee (Light)"] = ["Ближний бой (лёгкое оружие)", "Ближний бой (лёгкий)"],
        ["Arcana"] = ["Магия", "Аркана", "Тайная"],
        ["Divine"] = ["Вера", "Божественное", "Божественная"],
        ["Primal"] = ["Природа", "Первозданное", "Природная"],
        ["Runes"] = ["Руны"], ["Verse"] = ["Стих", "Песнь"],
    };

    public static string? BuiltInName(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var name = label.Trim();
        return Aliases.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)
            || pair.Value.Contains(name, StringComparer.OrdinalIgnoreCase)).Key;
    }

    /// <summary>Exact custom names take precedence; legacy aliases only resolve to built-ins.</summary>
    public static SkillDef? Resolve(IEnumerable<SkillDef> skills, string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var name = label.Trim();
        var list = skills.ToList();
        var exact = list.OrderBy(s => s.OwnerUserId == null).FirstOrDefault(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(s.NameRu, name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        var builtin = BuiltInName(name);
        return builtin == null ? null : list.FirstOrDefault(s => s.OwnerUserId == null
            && string.Equals(s.Name, builtin, StringComparison.OrdinalIgnoreCase));
    }
}
