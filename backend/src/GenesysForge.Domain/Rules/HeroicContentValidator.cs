using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Одна претензия к содержательности описаний героического контента.</summary>
/// <param name="Code">Стабильный код записи (у улучшения — код способности с уровнем).</param>
/// <param name="Problem">Машинный код проблемы.</param>
/// <param name="Message">Пояснение для лога и теста.</param>
public sealed record HeroicContentIssue(string Code, string Problem, string Message);

/// <summary>
/// Полнота описаний героических способностей, их улучшений и вторичных эффектов (ROT-HA-CONTENT).
/// Проверяются три вещи: полный RU-парафраз существует и не является заглушкой, EN-парафраз тоже
/// есть, и короткая safe-сводка отличается от полного текста — иначе полный текст правила утечёт
/// в <c>PublicSafe</c>. Смысл текста валидатор не оценивает.
/// </summary>
public static class HeroicContentValidator
{
    public const string ProblemMissingRu = "heroic.content.missing_ru";
    public const string ProblemMissingEn = "heroic.content.missing_en";
    public const string ProblemMissingSafe = "heroic.content.missing_safe";
    public const string ProblemSafeLeaksFull = "heroic.content.safe_leaks_full";
    public const string ProblemPlaceholder = "heroic.content.placeholder";
    public const string ProblemTooShort = "heroic.content.too_short";

    /// <summary>Минимальная длина осмысленного парафраза правила.</summary>
    private const int MinimumLength = 40;

    private static readonly string[] Placeholders =
    [
        "уточняется", "описание отсутствует", "нет описания", "tbd", "todo",
        "description pending", "no description",
    ];

    /// <summary>
    /// Все претензии к встроенным записям. Пустой список — контент полон.
    /// Кастомные способности (<c>OwnerUserId != null</c>) не проверяются: их пишет владелец;
    /// вторичные эффекты бывают только встроенными.
    /// </summary>
    public static IReadOnlyList<HeroicContentIssue> Validate(
        IEnumerable<HeroicAbilityDef> abilities,
        IEnumerable<HeroicSecondaryEffectDef> secondaryEffects)
    {
        var issues = new List<HeroicContentIssue>();

        foreach (var ability in abilities.Where(a => a.OwnerUserId is null && !a.Retired))
        {
            Check(issues, ability.Code, ability.Description, ability.DescriptionEn, ability.SafeDescription);
            foreach (var upgrade in ability.Upgrades.OrderBy(u => u.Level))
                Check(issues, $"{ability.Code}:{upgrade.Level}",
                    upgrade.Description, upgrade.DescriptionEn, upgrade.SafeDescription);
        }

        foreach (var effect in secondaryEffects.Where(e => !e.Retired))
            Check(issues, effect.Code, effect.Description, effect.DescriptionEn, effect.SafeDescription);

        return issues;
    }

    private static void Check(List<HeroicContentIssue> issues, string code, string? ru, string? en, string? safe)
    {
        if (string.IsNullOrWhiteSpace(ru))
        {
            issues.Add(new(code, ProblemMissingRu, "Нет полного русского парафраза."));
        }
        else
        {
            if (ru.Trim().Length < MinimumLength)
                issues.Add(new(code, ProblemTooShort,
                    $"Русский парафраз короче {MinimumLength} символов — правило по нему не разрешить."));
            if (IsPlaceholder(ru))
                issues.Add(new(code, ProblemPlaceholder, "Русский текст — заглушка, а не правило."));
        }

        if (string.IsNullOrWhiteSpace(en))
            issues.Add(new(code, ProblemMissingEn, "Нет английского парафраза."));
        else if (IsPlaceholder(en))
            issues.Add(new(code, ProblemPlaceholder, "Английский текст — заглушка, а не правило."));

        if (string.IsNullOrWhiteSpace(safe))
            issues.Add(new(code, ProblemMissingSafe, "Нет короткой safe-сводки для публичного режима."));
        else if (!string.IsNullOrWhiteSpace(ru) && safe.Trim() == ru.Trim())
            issues.Add(new(code, ProblemSafeLeaksFull,
                "Safe-сводка совпадает с полным текстом — правило утечёт в PublicSafe."));
    }

    private static bool IsPlaceholder(string text)
    {
        var lower = text.ToLowerInvariant();
        return Array.Exists(Placeholders, p => lower.Contains(p, StringComparison.Ordinal));
    }
}
