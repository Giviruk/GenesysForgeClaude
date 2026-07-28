using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Качество предмета после всех поправок: код справочника и итоговый рейтинг.</summary>
public sealed record EffectiveQuality(string Code, int Rating);

/// <summary>Кости умения, которые улучшение даёт конкретной проверке (Позолота, Руна сумерек).</summary>
public sealed record AttachmentSkillBoost(string SkillName, int Boost, string SourceName, string SourceNameRu);

/// <summary>
/// Правило улучшения, которое приложение не исполняет: автоматические символы и эффекты, которым
/// нужен рантайм столкновения. Показывается игроку — молча терять правило нельзя.
/// </summary>
public sealed record AttachmentNote(string SourceName, string SourceNameRu, string Text);

/// <summary>Сводный вклад установленных улучшений в характеристики предмета.</summary>
public sealed record AttachmentAggregate(
    int Encumbrance,
    int SoakBonus,
    int MeleeDefense,
    int RangedDefense,
    int DamageBonus,
    int CritReduction,
    IReadOnlyList<AttachmentSkillBoost> SkillBoosts,
    IReadOnlyList<AttachmentNote> Notes)
{
    public static readonly AttachmentAggregate Empty =
        new(0, 0, 0, 0, 0, 0, [], []);
}

/// <summary>Улучшение на входе расчёта: определение и то, действует ли оно сейчас.</summary>
/// <param name="WornAndActive">Предмет надет и выбран активной бронёй (ROT-CMB-02).</param>
public sealed record AttachmentInput(AttachmentDef Def, bool WornAndActive);

/// <summary>
/// Улучшения предметов (ROT-EQP-ATT-01): слоты, совместимость и типизированные эффекты.
/// Правило одно на всё приложение — лист, карточка и расчёт боя обязаны считать одинаково.
/// </summary>
public static class AttachmentRules
{
    /// <summary>
    /// Слоты улучшений записи без книжного значения: Core-запасной вариант «половина базового веса,
    /// вверх». Вес ноль — слотов ноль. Считается от базового веса, до поправок качества изготовления.
    /// </summary>
    public static int FallbackHardPoints(int baseEncumbrance) =>
        baseEncumbrance <= 0 ? 0 : (int)Math.Ceiling(baseEncumbrance / 2.0);

    /// <summary>Слоты предмета: книжное значение, а если его нет — Core-запасной расчёт от веса.</summary>
    public static int HardPoints(int? bookHardPoints, int baseEncumbrance) =>
        bookHardPoints ?? FallbackHardPoints(baseEncumbrance);

    /// <summary>Занято слотов установленными улучшениями.</summary>
    public static int UsedHardPoints(IEnumerable<AttachmentDef> installed) =>
        installed.Sum(a => a.HardPointCost);

    /// <summary>Свободные слоты; отрицательным не бывает даже у legacy-предмета с перебором.</summary>
    public static int RemainingHardPoints(int effectiveHardPoints, IEnumerable<AttachmentDef> installed) =>
        Math.Max(0, effectiveHardPoints - UsedHardPoints(installed));

    /// <summary>Предмет уже несёт больше улучшений, чем позволяют слоты (после уменьшения HP).</summary>
    public static bool IsOverCapacity(int effectiveHardPoints, IEnumerable<AttachmentDef> installed) =>
        UsedHardPoints(installed) > effectiveHardPoints;

    /// <summary>Улучшение подходит предмету по виду и признакам формы.</summary>
    public static bool IsCompatible(ItemKind hostKind, WeaponFormTraits hostTraits, AttachmentDef def)
    {
        if (hostKind != def.HostKind) return false;
        if ((def.RequiredTraits & hostTraits) != def.RequiredTraits) return false;
        if (def.RequiredAnyTraits != WeaponFormTraits.None
            && (def.RequiredAnyTraits & hostTraits) == WeaponFormTraits.None) return false;
        return (def.ForbiddenTraits & hostTraits) == WeaponFormTraits.None;
    }

    /// <summary>
    /// Полная проверка установки. Всё проверяется до изменения состояния: место, совместимость,
    /// повтор и требование магического навыка для чар.
    /// </summary>
    /// <param name="installed">Улучшения, уже стоящие на предмете.</param>
    /// <param name="installerHasMagicRank">У владельца есть хотя бы один ранг магического навыка.</param>
    /// <param name="overrideReason">
    /// Причина, по которой чары ставятся без магического навыка. Пусто — обычная проверка.
    /// </param>
    public static void EnsureCanInstall(
        ItemKind hostKind,
        WeaponFormTraits hostTraits,
        int effectiveHardPoints,
        IReadOnlyList<AttachmentDef> installed,
        AttachmentDef def,
        bool installerHasMagicRank,
        string? overrideReason = null)
    {
        if (!IsCompatible(hostKind, hostTraits, def))
            throw new DomainRuleException(
                "Это улучшение не подходит выбранному предмету.", "attachment.incompatible");

        // Два одинаковых улучшения на одном предмете не ставятся (решение владельца): повторный
        // эффект всё равно не складывался бы, а слоты тратились бы впустую.
        if (installed.Any(a => string.Equals(a.Code, def.Code, StringComparison.Ordinal)))
            throw new DomainRuleException(
                "Такое улучшение на этом предмете уже стоит.", "attachment.duplicate");

        if (RemainingHardPoints(effectiveHardPoints, installed) < def.HardPointCost)
            throw new DomainRuleException(
                "Не хватает слотов улучшений.", "attachment.no_hard_points");

        if (def.IsEnchantment && !installerHasMagicRank && string.IsNullOrWhiteSpace(overrideReason))
            throw new DomainRuleException(
                "Чары ставит только тот, у кого есть ранг магического навыка.",
                "attachment.magic_rank_required");
    }

    /// <summary>
    /// Сводит вклад установленных улучшений в числа предмета. Эффекты «только на надетой активной
    /// броне» в сумму не попадают, пока броня не надета: это то же правило, что у штрафов брони.
    /// </summary>
    public static AttachmentAggregate Aggregate(IEnumerable<AttachmentInput> attachments)
    {
        int enc = 0, soak = 0, melee = 0, ranged = 0, damage = 0, crit = 0;
        var boosts = new List<AttachmentSkillBoost>();
        var notes = new List<AttachmentNote>();

        foreach (var (def, wornActive) in attachments.Select(a => (a.Def, a.WornAndActive)))
            foreach (var effect in def.Effects)
            {
                // Правило показывается всегда — иначе игрок не узнает, что оно есть.
                if (effect.Kind is AttachmentEffectKind.NarrativeOnly or AttachmentEffectKind.AutomaticSymbol)
                {
                    notes.Add(new AttachmentNote(def.Name, def.NameRu, effect.Note));
                    continue;
                }
                if (effect.Condition == AttachmentEffectCondition.WornAndActive && !wornActive) continue;

                switch (effect.Kind)
                {
                    case AttachmentEffectKind.Encumbrance: enc += effect.Value; break;
                    case AttachmentEffectKind.Soak: soak += effect.Value; break;
                    case AttachmentEffectKind.MeleeDefense: melee += effect.Value; break;
                    case AttachmentEffectKind.RangedDefense: ranged += effect.Value; break;
                    case AttachmentEffectKind.Damage: damage += effect.Value; break;
                    case AttachmentEffectKind.CritReduction: crit += effect.Value; break;
                    case AttachmentEffectKind.SkillBoost:
                        boosts.Add(new AttachmentSkillBoost(effect.SkillName, effect.Value, def.Name, def.NameRu));
                        break;
                }
            }

        return new AttachmentAggregate(enc, soak, melee, ranged, damage, crit, boosts, notes);
    }

    /// <summary>
    /// Итоговые качества предмета: базовые плюс то, что делают улучшения. Порядок операций внутри
    /// одного предмета не важен — повторов одного улучшения не бывает, а «не ниже» и «плюс один»
    /// применяются к базовому набору.
    /// </summary>
    public static IReadOnlyList<EffectiveQuality> ApplyQualities(
        IEnumerable<EffectiveQuality> baseQualities, IEnumerable<AttachmentInput> attachments)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var q in baseQualities) result[q.Code] = q.Rating;

        foreach (var (def, wornActive) in attachments.Select(a => (a.Def, a.WornAndActive)))
            foreach (var effect in def.Effects)
            {
                if (effect.Condition == AttachmentEffectCondition.WornAndActive && !wornActive) continue;
                var code = effect.QualityCode;
                switch (effect.Kind)
                {
                    case AttachmentEffectKind.GrantOrIncreaseQuality:
                        result[code] = result.TryGetValue(code, out var current)
                            ? current + effect.Increment
                            : effect.Value;
                        break;

                    case AttachmentEffectKind.SetQualityAtLeast:
                        result[code] = result.TryGetValue(code, out var existing)
                            ? Math.Max(existing, effect.Value)
                            : effect.Value;
                        break;

                    case AttachmentEffectKind.GrantQualityOrCancelOpposite:
                        // Балансирный эфес: у неточного оружия он снимает помеху, а не добавляет
                        // бонусный куб поверх неё.
                        if (result.TryGetValue(effect.OppositeQualityCode, out var opposite) && opposite > 0)
                        {
                            var reduced = Math.Max(0, opposite - effect.Value);
                            if (reduced == 0) result.Remove(effect.OppositeQualityCode);
                            else result[effect.OppositeQualityCode] = reduced;
                        }
                        else
                        {
                            result[code] = result.TryGetValue(code, out var own)
                                ? own + effect.Value
                                : effect.Value;
                        }
                        break;
                }
            }

        return [.. result.Select(kv => new EffectiveQuality(kv.Key, kv.Value))
            .OrderBy(q => q.Code, StringComparer.Ordinal)];
    }
}
