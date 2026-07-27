namespace GenesysForge.Domain.Rules;

/// <summary>
/// Один дополнительный удар атаки (Auto-fire, Linked, несколько целей). Каждый удар проходит
/// поглощение отдельно, поэтому у него собственное поглощение цели.
/// </summary>
/// <param name="TargetSoak">Поглощение цели этого удара.</param>
/// <param name="Label">Подпись для лога: цель или источник дополнительного удара.</param>
public sealed record CombatHitInput(int TargetSoak, string Label = "");

/// <summary>
/// Трата символа, которую игрок хочет применить (качество, критическое ранение, narrative-эффект).
/// </summary>
/// <param name="Code">Стабильный код эффекта.</param>
/// <param name="MayActivateOnMiss">
/// Структурное правило эффекта прямо разрешает активацию при промахе (отдельные режимы Blast,
/// Sunder, Guided). По умолчанию активное качество требует попадания.
/// </param>
/// <param name="RequiresDamageThroughSoak">
/// Эффекту нужно, чтобы после поглощения прошёл хотя бы 1 урон — так работает обычное
/// критическое ранение.
/// </param>
public sealed record CombatSymbolSpend(
    string Code,
    bool MayActivateOnMiss = false,
    bool RequiresDamageThroughSoak = false);

/// <summary>Вход разрешения атаки: нетто-символы броска, профиль оружия и цели.</summary>
/// <param name="NetSuccesses">
/// Успехи после сокращения с провалами. Ноль или меньше — промах. Успех внутри Triumph уже учтён
/// сокращением; оставшийся Triumph промах в попадание не превращает.
/// </param>
/// <param name="NetAdvantages">Преимущества после сокращения — тратятся и при промахе.</param>
/// <param name="Triumphs">Оставшиеся Triumph.</param>
/// <param name="Despairs">Оставшиеся Despair.</param>
/// <param name="BaseDamage">Базовый урон профиля (для ближнего боя — уже с учётом Мощи).</param>
/// <param name="TargetSoak">Поглощение основной цели.</param>
/// <param name="AdditionalHits">Дополнительные удары той же атаки.</param>
/// <param name="RequestedSpends">Траты символов, выбранные игроком.</param>
/// <param name="Qualities">
/// Качества атакующего профиля (GEN-EQP-QUAL-01). Из них считаются игнорируемое поглощение
/// (Проникающее, Бронебойное) и прибавка к броску критического ранения (Высококритичное).
/// </param>
/// <param name="TargetReinforced">
/// У цели укреплённая броня: её поглощение не поддаётся Проникающему и Бронебойному.
/// </param>
public sealed record CombatAttackInput(
    int NetSuccesses,
    int BaseDamage,
    int TargetSoak,
    int NetAdvantages = 0,
    int Triumphs = 0,
    int Despairs = 0,
    IReadOnlyList<CombatHitInput>? AdditionalHits = null,
    IReadOnlyList<CombatSymbolSpend>? RequestedSpends = null,
    IReadOnlyList<WeaponQualityInput>? Qualities = null,
    bool TargetReinforced = false);

/// <summary>
/// Урон одного удара после поглощения.
/// </summary>
/// <param name="TargetSoak">Поглощение цели после Проникающего и Бронебойного.</param>
/// <param name="IgnoredSoak">Сколько поглощения качества сняли; 0 — не снимали ничего.</param>
public sealed record CombatHitResult(
    int RawDamage, int TargetSoak, int Applied, string Label, int IgnoredSoak = 0);

/// <summary>
/// Результат разрешения атаки. При промахе поля урона пусты, а не равны базовому урону:
/// показывать базовый урон на промахе — та самая ошибка, ради которой правило и вынесено на сервер.
/// </summary>
/// <param name="CriticalRollBonus">
/// Прибавка к броску критического ранения от Высококритичного (GEN-EQP-QUAL-01). Само ранение
/// качество не создаёт и его цену не снижает.
/// </param>
public sealed record CombatAttackResult(
    bool IsHit,
    int? RawDamagePerHit,
    IReadOnlyList<CombatHitResult> Hits,
    int TotalApplied,
    IReadOnlyList<string> AllowedSymbolSpends,
    IReadOnlyList<string> RejectedSymbolSpends,
    IReadOnlyList<string> Log,
    int CriticalRollBonus = 0);

/// <summary>
/// Разрешение атаки по Core (ROT-CMB-01): попадание, урон и допустимые траты символов.
/// Источник истины — сервер; клиентская сводка авторитетной не является.
/// </summary>
public static class CombatResolver
{
    public static CombatAttackResult Resolve(CombatAttackInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.BaseDamage < 0)
            throw new DomainRuleException("Базовый урон не может быть отрицательным.", "combat.base_damage_negative");
        if (input.TargetSoak < 0)
            throw new DomainRuleException("Поглощение не может быть отрицательным.", "combat.soak_negative");

        var log = new List<string>();
        var isHit = input.NetSuccesses > 0;
        var criticalBonus = WeaponQualityRules.CriticalRollBonus(input.Qualities);

        if (!isHit)
        {
            log.Add(input.Triumphs > 0
                ? "Промах: успехов не осталось. Оставшийся триумф попаданием не делает."
                : "Промах: успехов не осталось — обычный урон не применяется.");

            var (allowedOnMiss, rejectedOnMiss) = SplitSpends(input, isHit: false, anyDamageThroughSoak: false, log);
            return new CombatAttackResult(false, null, [], 0, allowedOnMiss, rejectedOnMiss, log);
        }

        // Попадание: каждый оставшийся успех добавляет 1 к базовому урону — один раз на атаку,
        // а не на каждый удар.
        var raw = input.BaseDamage + input.NetSuccesses;
        log.Add($"Попадание: базовый урон {input.BaseDamage} + успехов {input.NetSuccesses} = {raw}.");

        var hits = new List<CombatHitResult>
        {
            Hit(raw, input.TargetSoak, "Основная цель", input),
        };
        foreach (var extra in input.AdditionalHits ?? [])
        {
            if (extra.TargetSoak < 0)
                throw new DomainRuleException(
                    "Поглощение дополнительной цели не может быть отрицательным.", "combat.soak_negative");
            hits.Add(Hit(raw, extra.TargetSoak,
                string.IsNullOrWhiteSpace(extra.Label) ? "Дополнительный удар" : extra.Label, input));
        }

        // Поглощение применяется к каждому удару отдельно, а не к их сумме.
        foreach (var hit in hits)
            log.Add(hit.IgnoredSoak > 0
                ? $"{hit.Label}: {hit.RawDamage} − поглощение {hit.TargetSoak} " +
                  $"(снято качествами {hit.IgnoredSoak}) = {hit.Applied}."
                : $"{hit.Label}: {hit.RawDamage} − поглощение {hit.TargetSoak} = {hit.Applied}.");
        if (input.TargetReinforced && WeaponQualityRules.EffectiveSoak(10, input.Qualities) < 10)
            log.Add("Броня цели укреплена: Проникающее и Бронебойное её поглощение не снимают.");
        if (criticalBonus > 0)
            log.Add($"Высококритичное: +{criticalBonus} к броску критического ранения.");

        var total = hits.Sum(h => h.Applied);
        var anyThroughSoak = hits.Exists(h => h.Applied > 0);
        if (!anyThroughSoak)
            log.Add("Весь урон поглощён: обычное критическое ранение недоступно.");

        var (allowed, rejected) = SplitSpends(input, isHit: true, anyThroughSoak, log);
        return new CombatAttackResult(true, raw, hits, total, allowed, rejected, log, criticalBonus);
    }

    /// <summary>
    /// Один удар: поглощение цели уменьшается качествами оружия (Проникающее, Бронебойное), но
    /// не ниже нуля и не у укреплённой брони.
    /// </summary>
    private static CombatHitResult Hit(int raw, int soak, string label, CombatAttackInput input)
    {
        var effective = WeaponQualityRules.EffectiveSoak(soak, input.Qualities, input.TargetReinforced);
        return new CombatHitResult(raw, effective, Math.Max(0, raw - effective), label, soak - effective);
    }

    /// <summary>
    /// Делит запрошенные траты на допустимые и отклонённые. Активное качество по умолчанию требует
    /// попадания; исключение — только эффект, чьё структурное правило прямо это разрешает.
    /// </summary>
    private static (List<string> Allowed, List<string> Rejected) SplitSpends(
        CombatAttackInput input, bool isHit, bool anyDamageThroughSoak, List<string> log)
    {
        var allowed = new List<string>();
        var rejected = new List<string>();

        foreach (var spend in input.RequestedSpends ?? [])
        {
            if (!isHit && !spend.MayActivateOnMiss)
            {
                rejected.Add(spend.Code);
                log.Add($"«{spend.Code}» требует попадания — трата отклонена.");
                continue;
            }
            if (spend.RequiresDamageThroughSoak && !anyDamageThroughSoak)
            {
                rejected.Add(spend.Code);
                log.Add($"«{spend.Code}» требует прошедшего сквозь поглощение урона — трата отклонена.");
                continue;
            }
            allowed.Add(spend.Code);
        }

        return (allowed, rejected);
    }
}
