using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Одна позиция разрешённого карьерного комплекта.</summary>
public sealed record CareerPackageLine(string ItemCode, string ItemNameFallback, int Quantity);

/// <summary>Один слот выбора комплекта: группа и её допустимые варианты.</summary>
public sealed record CareerPackageSlot(string ChoiceGroup, IReadOnlyList<int> OptionIndexes);

/// <summary>Отказ резолвера с машинным кодом причины — не текст для сравнения на клиенте.</summary>
public sealed record CareerPackageError(string ReasonCode, string Message);

/// <summary>
/// Разрешение карьерного комплекта (ROT-CRE-03). Комплект выдаётся только целиком: все
/// фиксированные позиции, ровно одна опция каждой группы выбора, без дублей и без опций
/// чужой карьеры. Любое нарушение — отказ до первой мутации, частичная выдача невозможна.
/// </summary>
public static class CareerPackageResolver
{
    public const string ReasonNoPackage = "career.package.not_available";
    public const string ReasonMissingGroup = "career.package.group_missing";
    public const string ReasonUnknownGroup = "career.package.group_unknown";
    public const string ReasonDuplicateGroup = "career.package.group_duplicated";
    public const string ReasonUnknownOption = "career.package.option_unknown";

    /// <summary>Слоты выбора комплекта в стабильном порядке (по имени группы).</summary>
    public static IReadOnlyList<CareerPackageSlot> Slots(IEnumerable<CareerStartingGear> gear) => gear
        .Where(g => g.IsChoice)
        .GroupBy(g => g.ChoiceGroup, StringComparer.Ordinal)
        .OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => new CareerPackageSlot(g.Key, g.Select(x => x.ChoiceOption).Distinct().Order().ToList()))
        .ToList();

    /// <summary>
    /// Проверяет выбор игрока и раскладывает комплект в плоский список позиций.
    /// </summary>
    /// <param name="gear">Полное стартовое снаряжение карьеры.</param>
    /// <param name="picks">Выбор игрока: группа → индекс опции. Дубли групп обязан отсеять вызывающий.</param>
    /// <param name="duplicateGroups">Группы, встретившиеся в запросе более одного раза.</param>
    public static (IReadOnlyList<CareerPackageLine>? Lines, CareerPackageError? Error) Resolve(
        IReadOnlyCollection<CareerStartingGear> gear,
        IReadOnlyDictionary<string, int> picks,
        IReadOnlyCollection<string> duplicateGroups)
    {
        if (gear.Count == 0)
            return (null, new CareerPackageError(ReasonNoPackage,
                "У этой карьеры нет стартового комплекта; доступен только режим стандартных денег."));

        if (duplicateGroups.Count > 0)
            return (null, new CareerPackageError(ReasonDuplicateGroup,
                $"Группа выбора указана дважды: {string.Join(", ", duplicateGroups.Order())}."));

        var slots = Slots(gear);
        var known = slots.Select(s => s.ChoiceGroup).ToHashSet(StringComparer.Ordinal);

        var unknown = picks.Keys.Where(k => !known.Contains(k)).Order().ToList();
        if (unknown.Count > 0)
            return (null, new CareerPackageError(ReasonUnknownGroup,
                $"Неизвестная группа стартового снаряжения: {string.Join(", ", unknown)}."));

        var missing = slots.Where(s => !picks.ContainsKey(s.ChoiceGroup)).Select(s => s.ChoiceGroup).ToList();
        if (missing.Count > 0)
            return (null, new CareerPackageError(ReasonMissingGroup,
                $"Комплект выдаётся целиком: не выбран вариант для {string.Join(", ", missing)}."));

        var lines = gear.Where(g => !g.IsChoice)
            .Select(g => new CareerPackageLine(g.ItemCode, g.ItemNameFallback, g.Quantity))
            .ToList();

        foreach (var slot in slots)
        {
            var option = picks[slot.ChoiceGroup];
            if (!slot.OptionIndexes.Contains(option))
                return (null, new CareerPackageError(ReasonUnknownOption,
                    $"Недопустимый вариант {option} для группы {slot.ChoiceGroup}."));

            lines.AddRange(gear
                .Where(g => g.IsChoice && g.ChoiceGroup == slot.ChoiceGroup && g.ChoiceOption == option)
                .Select(g => new CareerPackageLine(g.ItemCode, g.ItemNameFallback, g.Quantity)));
        }

        // Одинаковые коды из разных строк складываются: «Sword + Dagger» и фиксированный Dagger
        // должны дать одну позицию с суммарным количеством, а не два одинаковых предмета.
        var merged = lines
            .GroupBy(l => l.ItemCode, StringComparer.Ordinal)
            .Select(g => new CareerPackageLine(g.Key, g.First().ItemNameFallback, g.Sum(x => x.Quantity)))
            .ToList();

        return (merged, null);
    }
}
