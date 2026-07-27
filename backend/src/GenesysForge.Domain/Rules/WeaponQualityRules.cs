namespace GenesysForge.Domain.Rules;

/// <summary>Качество в расчёте атаки: имена для подсказки, типизированная механика и рейтинг.</summary>
public sealed record WeaponQualityInput(
    string NameEn, string NameRu, QualityEffectKind Kind, int Rating = 0);

/// <summary>Один вклад качества в пул атаки — чтобы игрок видел, откуда взялся куб.</summary>
/// <param name="Boost">Бонусные кости.</param>
/// <param name="Setback">Кости помех.</param>
/// <param name="Difficulty">На сколько выросла сложность.</param>
/// <param name="Advantage">Автоматические преимущества.</param>
/// <param name="Threat">Автоматические угрозы.</param>
public sealed record QualityContribution(
    string NameEn, string NameRu, int Boost = 0, int Setback = 0, int Difficulty = 0,
    int Advantage = 0, int Threat = 0);

/// <summary>
/// Что качества оружия делают с пулом атаки до броска (GEN-EQP-QUAL-01). Автоматических
/// преимуществ и угроз в пуле нет — они прибавляются к результату, поэтому едут отдельными полями.
/// </summary>
public sealed record AttackPoolModifiers(
    int Boost,
    int Setback,
    int DifficultyIncrease,
    int AutomaticAdvantage,
    int AutomaticThreat,
    IReadOnlyList<QualityContribution> Sources)
{
    public static readonly AttackPoolModifiers None = new(0, 0, 0, 0, 0, []);
}

/// <summary>
/// Исполняемая часть качеств оружия и брони. До этого качество было надписью на карточке:
/// игрок сам помнил, что латный щит громоздкий, а лук неточен, и сам добавлял кубы.
/// </summary>
public static class WeaponQualityRules
{
    /// <summary>
    /// Сводит качества профиля к изменению пула. Громоздкое и Сноровка считаются от нехватки
    /// характеристики: при Мощи 2 и Громоздком 4 сложность растёт на две ступени.
    /// </summary>
    public static AttackPoolModifiers PoolFor(
        IEnumerable<WeaponQualityInput>? qualities, int brawn, int agility)
    {
        var sources = new List<QualityContribution>();

        foreach (var q in qualities ?? [])
        {
            var rating = Math.Max(0, q.Rating);
            var contribution = q.Kind switch
            {
                QualityEffectKind.AttackBoost when rating > 0 =>
                    new QualityContribution(q.NameEn, q.NameRu, Boost: rating),
                QualityEffectKind.AttackSetback when rating > 0 =>
                    new QualityContribution(q.NameEn, q.NameRu, Setback: rating),
                QualityEffectKind.DifficultyPerMissingBrawn when rating > brawn =>
                    new QualityContribution(q.NameEn, q.NameRu, Difficulty: rating - Math.Max(0, brawn)),
                QualityEffectKind.DifficultyPerMissingAgility when rating > agility =>
                    new QualityContribution(q.NameEn, q.NameRu, Difficulty: rating - Math.Max(0, agility)),
                QualityEffectKind.AutomaticAdvantage =>
                    new QualityContribution(q.NameEn, q.NameRu, Advantage: 1),
                QualityEffectKind.AutomaticThreat =>
                    new QualityContribution(q.NameEn, q.NameRu, Threat: 1),
                _ => null,
            };
            if (contribution is not null) sources.Add(contribution);
        }

        return new AttackPoolModifiers(
            sources.Sum(s => s.Boost),
            sources.Sum(s => s.Setback),
            sources.Sum(s => s.Difficulty),
            sources.Sum(s => s.Advantage),
            sources.Sum(s => s.Threat),
            sources);
    }

    /// <summary>
    /// Поглощение цели после Проникающего и Бронебойного. Укреплённая броня им не поддаётся —
    /// именно поэтому признак цели передаётся отдельно, а не выводится из её поглощения.
    /// </summary>
    /// <param name="targetSoak">Поглощение цели.</param>
    /// <param name="qualities">Качества атакующего профиля.</param>
    /// <param name="targetReinforced">У цели укреплённая броня.</param>
    public static int EffectiveSoak(
        int targetSoak, IEnumerable<WeaponQualityInput>? qualities, bool targetReinforced = false)
    {
        if (targetSoak < 0) throw new ArgumentOutOfRangeException(nameof(targetSoak));
        if (targetReinforced) return targetSoak;

        var ignored = 0;
        foreach (var q in qualities ?? [])
        {
            var rating = Math.Max(0, q.Rating);
            ignored += q.Kind switch
            {
                QualityEffectKind.IgnoreSoak => rating,
                QualityEffectKind.IgnoreSoakTenfold => rating * 10,
                _ => 0,
            };
        }

        return Math.Max(0, targetSoak - ignored);
    }

    /// <summary>
    /// Прибавка к броску критического ранения: Высококритичное даёт по десять за ранг. Само
    /// критическое ранение качество не создаёт и его цену не снижает.
    /// </summary>
    public static int CriticalRollBonus(IEnumerable<WeaponQualityInput>? qualities) =>
        (qualities ?? [])
            .Where(q => q.Kind == QualityEffectKind.CriticalBonusTenfold)
            .Sum(q => Math.Max(0, q.Rating) * 10);

    /// <summary>Есть ли у набора качеств признак укреплённости (иммунитет к Pierce/Breach/Sunder).</summary>
    public static bool IsReinforced(IEnumerable<WeaponQualityInput>? qualities) =>
        (qualities ?? []).Any(q => q.Kind == QualityEffectKind.ImmuneToPierceAndSunder);
}
