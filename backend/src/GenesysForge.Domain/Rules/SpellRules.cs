namespace GenesysForge.Domain.Rules;

/// <summary>
/// Разбор строк сложности справочника магии. В каталоге они печатные: у базового эффекта
/// «1 (Easy)», у дополнительного «+1». Числа из них нужны и серверу, и клиенту, поэтому правило
/// одно и живёт в домене, а не разъезжается по двум реализациям.
/// </summary>
public static class SpellRules
{
    /// <summary>
    /// Первое число строки: базовая сложность действия или надбавка дополнительного эффекта.
    /// Строка без числа — ноль, а не исключение: справочник пополняется людьми, и падать всем
    /// листом из-за одной записи нельзя.
    /// </summary>
    public static int ParseIncrease(string? difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty)) return 0;
        var digits = 0;
        var seen = false;
        foreach (var ch in difficulty)
        {
            if (char.IsDigit(ch)) { digits = digits * 10 + (ch - '0'); seen = true; continue; }
            if (seen) break;
        }
        return seen ? digits : 0;
    }
}
