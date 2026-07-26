namespace GenesysForge.Domain.Rules;

/// <summary>
/// Результат броска по таблице происхождения (ROT-HA-01). Хранит и итоговые категории, и
/// фактические грани — специальный «0» виден в аудите, но сам финальным происхождением не становится.
/// </summary>
/// <param name="Rolls">Грани d10 в порядке броска, как они напечатаны на кости: «0» — специальный результат.</param>
/// <param name="Primary">Первая категория происхождения.</param>
/// <param name="Secondary">Вторая категория; заполнена только после специального результата.</param>
public sealed record HeroicOriginRoll(
    IReadOnlyList<int> Rolls,
    HeroicOriginType Primary,
    HeroicOriginType? Secondary)
{
    public HeroicOriginMode Mode => Secondary is null ? HeroicOriginMode.Standard : HeroicOriginMode.DoubleStandard;
}

/// <summary>
/// Таблица происхождения героической способности: чистая доменная функция над инъецированным d10.
/// Бросок отделён от источника случайности, поэтому последовательность граней воспроизводима в тестах
/// и записывается в историю персонажа.
/// </summary>
public static class HeroicOriginTable
{
    /// <summary>Число граней таблицы.</summary>
    public const int Sides = 10;

    /// <summary>Напечатанная грань специального результата «бросить ещё два раза».</summary>
    public const int SpecialFace = 0;

    /// <summary>
    /// Предохранитель от бесконечного цикла на сломанном RNG: честная кость выдаёт подряд
    /// столько «0» с исчезающе малой вероятностью, поэтому это ошибка источника, а не игры.
    /// </summary>
    private const int MaxRolls = 100;

    /// <summary>Все категории таблицы в порядке граней 1–9.</summary>
    public static IReadOnlyList<HeroicOriginType> AllTypes { get; } =
        [.. Enum.GetValues<HeroicOriginType>().OrderBy(t => (int)t)];

    /// <summary>Грань 1–9 соответствует категории; «0»/10 — специальный результат, а не категория.</summary>
    public static bool TryMap(int face, out HeroicOriginType type)
    {
        if (face is >= 1 and <= 9)
        {
            type = (HeroicOriginType)face;
            return true;
        }
        type = default;
        return false;
    }

    /// <summary>
    /// Бросает таблицу. <paramref name="rollSides"/> получает число граней и обязан вернуть 1..sides
    /// (грань 10 — это напечатанный «0»). Специальный результат даёт ровно два обычных результата;
    /// каждый повторный «0» перебрасывается, одинаковые обычные результаты сохраняются как есть.
    /// </summary>
    public static HeroicOriginRoll Roll(Func<int, int> rollSides)
    {
        ArgumentNullException.ThrowIfNull(rollSides);

        var faces = new List<int>();
        var first = NextFace(rollSides, faces);
        if (TryMap(first, out var single))
            return new HeroicOriginRoll(faces, single, null);

        HeroicOriginType? a = null;
        HeroicOriginType? b = null;
        while (b is null)
        {
            var face = NextFace(rollSides, faces);
            if (!TryMap(face, out var type)) continue; // повторный «0» перебрасывается
            if (a is null) a = type; else b = type;
        }
        return new HeroicOriginRoll(faces, a!.Value, b);
    }

    private static int NextFace(Func<int, int> rollSides, List<int> faces)
    {
        if (faces.Count >= MaxRolls)
            throw new InvalidOperationException(
                $"Источник случайности вернул {MaxRolls} бросков без обычного результата таблицы происхождения.");

        var value = rollSides(Sides);
        if (value < 1 || value > Sides)
            throw new InvalidOperationException($"Бросок d{Sides} вернул {value} вне диапазона 1..{Sides}.");

        var face = value == Sides ? SpecialFace : value;
        faces.Add(face);
        return face;
    }
}
