using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Одна претензия к содержательности описания таланта.</summary>
/// <param name="Code">Стабильный код записи.</param>
/// <param name="Problem">Машинный код проблемы.</param>
/// <param name="Message">Пояснение для лога и теста.</param>
public sealed record TalentContentIssue(string Code, string Problem, string Message);

/// <summary>
/// Проверка полноты описаний талантов (ROT-TAL-07). Каждый активный талант обязан иметь
/// содержательные RU и EN парафразы; заглушка, пустой текст или совпадение локалей — ошибка.
/// Валидатор не проверяет смысл текста, только его наличие и отличимость от заглушки.
/// </summary>
public static class TalentContentValidator
{
    public const string ProblemMissingRu = "talent.content.missing_ru";
    public const string ProblemMissingEn = "talent.content.missing_en";
    public const string ProblemPlaceholder = "talent.content.placeholder";
    public const string ProblemTooShort = "talent.content.too_short";
    public const string ProblemLocalesIdentical = "talent.content.locales_identical";

    /// <summary>Минимальная длина осмысленного парафраза правила.</summary>
    private const int MinimumLength = 40;

    /// <summary>Тексты-заглушки, которые нельзя выдавать за описание правила.</summary>
    private static readonly string[] Placeholders =
    [
        "уточняется", "описание отсутствует", "нет описания", "tbd", "todo",
        "description pending", "no description",
    ];

    /// <summary>Все претензии к активным талантам; пустой список — контент полон.</summary>
    public static IReadOnlyList<TalentContentIssue> Validate(IEnumerable<TalentDef> talents)
    {
        var issues = new List<TalentContentIssue>();

        foreach (var talent in talents)
        {
            // Retired-записи не предлагаются к покупке, поэтому полнота их текста не требуется.
            if (talent.Retired) continue;

            var ru = talent.SafeDescription?.Trim() ?? "";
            var en = talent.DescriptionEn?.Trim() ?? "";

            if (ru.Length == 0)
            {
                issues.Add(new TalentContentIssue(talent.Code, ProblemMissingRu, $"«{talent.Name}»: нет русского описания."));
                continue;
            }
            if (en.Length == 0)
            {
                issues.Add(new TalentContentIssue(talent.Code, ProblemMissingEn, $"«{talent.Name}»: нет английского описания."));
                continue;
            }

            var placeholder = Placeholders.FirstOrDefault(marker =>
                ru.Contains(marker, StringComparison.OrdinalIgnoreCase)
                || en.Contains(marker, StringComparison.OrdinalIgnoreCase));
            if (placeholder is not null)
            {
                issues.Add(new TalentContentIssue(talent.Code, ProblemPlaceholder,
                    $"«{talent.Name}»: описание содержит заглушку «{placeholder}»."));
                continue;
            }

            if (ru.Length < MinimumLength || en.Length < MinimumLength)
            {
                issues.Add(new TalentContentIssue(talent.Code, ProblemTooShort,
                    $"«{talent.Name}»: описание короче {MinimumLength} символов и не раскрывает правило."));
                continue;
            }

            if (string.Equals(ru, en, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new TalentContentIssue(talent.Code, ProblemLocalesIdentical,
                    $"«{talent.Name}»: русский и английский тексты совпадают."));
            }
        }

        return issues;
    }
}
