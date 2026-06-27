using System.Text.RegularExpressions;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Best-effort разбор боевой строки снаряжения NPC вида
/// «Длинный меч (Урон +3, Крит 2, Средняя, Оборонительное 1)» в структурную атаку.
/// Строка считается атакой только если содержит маркер урона/крита; иначе это небоевое
/// снаряжение и парсер возвращает null (строка остаётся в Equipment без потери данных).
/// Чистая логика без зависимостей; используется бэкфиллом NpcAttack и тестами.
/// </summary>
public static partial class NpcEquipmentParser
{
    public readonly record struct ParsedAttack(
        string Name, string Damage, string Critical, string RangeBand,
        IReadOnlyList<ItemPropertyParser.Token> Qualities);

    [GeneratedRegex(@"(урон|damage)\s*([+]?\d+)", RegexOptions.IgnoreCase, "ru-RU")]
    private static partial Regex DamageToken();

    [GeneratedRegex(@"(крит|critical|crit)\.?\s*([+]?\d+)", RegexOptions.IgnoreCase, "ru-RU")]
    private static partial Regex CritToken();

    /// <summary>Известные русские/английские подписи дистанции (нижний регистр, ё→е).</summary>
    private static readonly string[] RangeBands =
        ["вплотную", "ближняя", "средняя", "дальняя", "очень дальняя", "engaged", "short", "medium", "long", "extreme"];

    /// <summary>Разбирает строку; возвращает атаку, если распознан урон или крит, иначе null.</summary>
    public static ParsedAttack? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        var paren = line.IndexOf('(');
        var name = (paren >= 0 ? line[..paren] : line).Trim();
        if (name.Length == 0) return null;

        var inside = paren >= 0 ? line[(paren + 1)..].TrimEnd(')', ' ') : "";
        if (inside.Length == 0) return null; // без скобки со статами — не атака

        string damage = "", critical = "", range = "";
        var qualities = new List<ItemPropertyParser.Token>();

        foreach (var part in inside.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var dm = DamageToken().Match(part);
            if (dm.Success) { damage = dm.Groups[2].Value; continue; }
            var cm = CritToken().Match(part);
            if (cm.Success) { critical = cm.Groups[2].Value; continue; }
            if (RangeBands.Contains(Norm(part))) { range = part.Trim(); continue; }
            // остаток — качество (имя + хвостовой рейтинг)
            foreach (var t in ItemPropertyParser.Parse(part)) qualities.Add(t);
        }

        // Атакой считаем только запись с уроном или критом.
        if (damage.Length == 0 && critical.Length == 0) return null;
        return new ParsedAttack(name, damage, critical, range, qualities);
    }

    private static string Norm(string s) => s.Trim().ToLowerInvariant().Replace('ё', 'е');
}
