namespace GenesysForge.Domain.Rules;

/// <summary>
/// Денежная формула стартового комплекта: фиксированная часть плюс необязательный бросок
/// вида <c>NdM</c> (например «1d100» или «200 + 1d100»). Разбор отделён от броска, чтобы
/// сервер мог показать формулу, выполнить её через инъецированный RNG и записать в audit
/// и саму формулу, и фактический результат.
/// </summary>
public readonly record struct MoneyFormula(int Fixed, int DiceCount, int DiceSides)
{
    public bool HasDice => DiceCount > 0 && DiceSides > 1;

    /// <summary>Минимально возможный результат — для preview и валидации.</summary>
    public int Minimum => Fixed + DiceCount;

    /// <summary>Максимально возможный результат — для preview и валидации.</summary>
    public int Maximum => Fixed + DiceCount * DiceSides;

    /// <summary>Отображаемая формула, напр. «200 + 1d100» или «1d100».</summary>
    public string Describe() => (Fixed, HasDice) switch
    {
        (0, true) => $"{DiceCount}d{DiceSides}",
        (_, true) => $"{Fixed} + {DiceCount}d{DiceSides}",
        _ => Fixed.ToString(),
    };

    /// <summary>
    /// Выполняет формулу: <paramref name="rollSide"/> вызывается ровно <see cref="DiceCount"/> раз
    /// и обязан возвращать 1..sides. Порядок бросков стабилен, поэтому результат воспроизводим.
    /// </summary>
    public int Roll(Func<int, int> rollSide)
    {
        ArgumentNullException.ThrowIfNull(rollSide);
        var total = Fixed;
        for (var i = 0; i < DiceCount; i++)
        {
            var value = rollSide(DiceSides);
            if (value < 1 || value > DiceSides)
                throw new InvalidOperationException($"Бросок d{DiceSides} вернул {value} вне диапазона 1..{DiceSides}.");
            total += value;
        }
        return total;
    }

    /// <summary>
    /// Разбирает пару «фиксированная часть» + «строка костей». Пустая/некорректная строка костей
    /// даёт формулу без броска, а не молчаливо испорченный результат — на невалидный текст
    /// возвращается <c>false</c>.
    /// </summary>
    public static bool TryParse(int fixedPart, string? dice, out MoneyFormula formula)
    {
        formula = new MoneyFormula(Math.Max(0, fixedPart), 0, 0);
        var text = dice?.Trim();
        if (string.IsNullOrEmpty(text)) return true;

        var parts = text.Split('d', StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var count)
            || !int.TryParse(parts[1], out var sides)
            || count < 1 || count > 100 || sides < 2 || sides > 1000)
            return false;

        formula = new MoneyFormula(Math.Max(0, fixedPart), count, sides);
        return true;
    }

    /// <summary>Разбор с ошибкой вместо тихого нуля: используется там, где формула — данные каталога.</summary>
    public static MoneyFormula Parse(int fixedPart, string? dice) =>
        TryParse(fixedPart, dice, out var formula)
            ? formula
            : throw new ArgumentException($"Некорректная денежная формула «{dice}».", nameof(dice));
}
