using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Одна претензия к записи справочника качеств.</summary>
public sealed record QualityContentIssue(string Code, string Problem, string Message);

/// <summary>
/// Проверка справочника качеств (GEN-EQP-QUAL-01): согласованность активности, стоимости и
/// рейтинга, а также отсутствие мусора от разбора CSV. Обрезанная на середине фразы строка с
/// висящей кавычкой — это не описание правила, а обломок импорта.
/// </summary>
public static class QualityContentValidator
{
    public const string ProblemQuoteArtifact = "quality.content.quote_artifact";
    public const string ProblemActiveWithoutCost = "quality.content.active_without_cost";
    public const string ProblemPassiveWithCost = "quality.content.passive_with_cost";
    public const string ProblemPassiveRequiresHit = "quality.content.passive_requires_hit";
    public const string ProblemRatingMismatch = "quality.content.rating_mismatch";
    public const string ProblemMissingRu = "quality.content.missing_ru";
    public const string ProblemMissingEn = "quality.content.missing_en";

    /// <summary>Механики, которым рейтинг обязателен: без числа правило посчитать нельзя.</summary>
    private static readonly QualityEffectKind[] RatedEffects =
    [
        QualityEffectKind.AttackBoost,
        QualityEffectKind.AttackSetback,
        QualityEffectKind.DifficultyPerMissingBrawn,
        QualityEffectKind.DifficultyPerMissingAgility,
        QualityEffectKind.DefenseMelee,
        QualityEffectKind.DefenseRanged,
        QualityEffectKind.IgnoreSoak,
        QualityEffectKind.IgnoreSoakTenfold,
        QualityEffectKind.CriticalBonusTenfold,
    ];

    /// <summary>Все претензии к справочнику; пустой список — справочник согласован.</summary>
    public static IReadOnlyList<QualityContentIssue> Validate(IEnumerable<QualityDef> qualities)
    {
        var issues = new List<QualityContentIssue>();

        foreach (var q in qualities)
        {
            if (q.Retired) continue;

            foreach (var (field, text) in new[]
                     {
                         ("activationCost", q.ActivationCost), ("category", q.Category),
                         ("description", q.Description), ("safeDescription", q.SafeDescription),
                     })
            {
                if (HasQuoteArtifact(text))
                    issues.Add(new QualityContentIssue(q.Code, ProblemQuoteArtifact,
                        $"Поле «{field}» начинается или заканчивается висящей кавычкой: «{text}»."));
            }

            if (q.IsActive && q.AdvantageCost <= 0)
                issues.Add(new QualityContentIssue(q.Code, ProblemActiveWithoutCost,
                    "Активное качество обязано иметь стоимость активации в преимуществах."));

            if (!q.IsActive && q.AdvantageCost != 0)
                issues.Add(new QualityContentIssue(q.Code, ProblemPassiveWithCost,
                    "Пассивное качество не активируется и не может стоить преимуществ."));

            if (!q.IsActive && (q.RequiresHit || q.CanActivateOnMiss || q.TriumphMayPay))
                issues.Add(new QualityContentIssue(q.Code, ProblemPassiveRequiresHit,
                    "Пассивное качество не активируется, поэтому флаги активации у него бессмысленны."));

            if (RatedEffects.Contains(q.EffectKind) && !q.HasRating)
                issues.Add(new QualityContentIssue(q.Code, ProblemRatingMismatch,
                    $"Механика «{q.EffectKind}» считается по рейтингу, а рейтинга у качества нет."));

            if (string.IsNullOrWhiteSpace(q.NameRu))
                issues.Add(new QualityContentIssue(q.Code, ProblemMissingRu, "Нет русского названия."));
            if (string.IsNullOrWhiteSpace(q.NameEn))
                issues.Add(new QualityContentIssue(q.Code, ProblemMissingEn, "Нет английского названия."));
        }

        return issues;
    }

    /// <summary>
    /// Висящая кавычка по краям — след неудачного разбора CSV: текст в этом месте оборван.
    /// Кавычки внутри предложения законны и претензией не считаются.
    /// </summary>
    private static bool HasQuoteArtifact(string? text)
    {
        var value = text?.Trim() ?? "";
        if (value.Length == 0) return false;
        var quotes = value.Count(c => c == '"');
        if (quotes == 0) return false;
        // Нечётное число кавычек означает незакрытую цитату; чётное по краям — тоже обломок.
        return quotes % 2 != 0 || value.StartsWith('"') || value.EndsWith('"');
    }
}
