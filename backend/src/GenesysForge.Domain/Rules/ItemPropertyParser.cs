using System.Text.RegularExpressions;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Разбор строки свойств предмета («Точное 1, Оборонительное 2») на токены имя+рейтинг.
/// Чистая логика без зависимостей; используется бэкфиллом структурных качеств и тестами.
/// </summary>
public static partial class ItemPropertyParser
{
    public readonly record struct Token(string Name, int? Rating);

    [GeneratedRegex(@"(\d+)\s*$")]
    private static partial Regex TrailingRating();

    public static IEnumerable<Token> Parse(string? properties)
    {
        if (string.IsNullOrWhiteSpace(properties)) yield break;
        foreach (var part in properties.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var m = TrailingRating().Match(part);
            var rating = m.Success ? int.Parse(m.Groups[1].Value) : (int?)null;
            var name = (m.Success ? part[..m.Index] : part).Trim();
            if (name.Length > 0) yield return new Token(name, rating);
        }
    }

    /// <summary>Нормализация имени свойства для сопоставления: нижний регистр, ё→е, без хвостового рейтинга.</summary>
    public static string Normalize(string name) =>
        TrailingRating().Replace(name.ToLowerInvariant().Replace('ё', 'е'), "").Trim();
}
