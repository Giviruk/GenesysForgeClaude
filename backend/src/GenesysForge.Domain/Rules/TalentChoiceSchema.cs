using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Схема обязательного выбора таланта (ROT-TAL-03): что и сколько игрок выбирает на ранг.
/// Схема версионируется, чтобы старые сохранённые выборы можно было отличить от новых правил.
/// </summary>
/// <param name="Kind">Тип выбираемого значения.</param>
/// <param name="CountForFirstRank">Сколько значений выбирается при покупке первого ранга.</param>
/// <param name="CountForNextRank">Сколько значений добавляет каждый следующий ранг.</param>
/// <param name="DistinctAcrossRanks">
/// Значения не должны повторяться между рангами. Действует только там, где таблица ТЗ прямо этого
/// требует: Dedication, Knack for It, Natural, Heroic Will.
/// </param>
/// <param name="AllowedSkillKinds">
/// Допустимые виды навыков для <see cref="TalentChoiceKind.Skill"/>. Пусто — ограничений нет.
/// </param>
/// <param name="Version">Версия схемы.</param>
public sealed record TalentChoiceSchema(
    TalentChoiceKind Kind,
    int CountForFirstRank,
    int CountForNextRank,
    bool DistinctAcrossRanks,
    IReadOnlyList<SkillKind> AllowedSkillKinds,
    int Version = 1)
{
    public static readonly TalentChoiceSchema None =
        new(TalentChoiceKind.None, 0, 0, false, [], Version: 1);

    /// <summary>Сколько значений требуется при покупке ранга с индексом <paramref name="rankIndex"/>.</summary>
    public int CountForRank(int rankIndex) => rankIndex == 0 ? CountForFirstRank : CountForNextRank;

    public bool Required => Kind != TalentChoiceKind.None;
}

/// <summary>Отказ валидации выбора с машинным кодом причины.</summary>
public sealed record TalentChoiceError(string ReasonCode, string Message);

/// <summary>
/// Схемы выбора для талантов RoT и проверка присланных значений (ROT-TAL-03).
/// Валидация выполняется до списания XP: невалидный запрос не меняет ничего.
/// </summary>
public static class TalentChoiceSchemas
{
    public const string ReasonMissing = "talent.choice.required";
    public const string ReasonCount = "talent.choice.wrong_count";
    public const string ReasonDuplicate = "talent.choice.duplicate";
    public const string ReasonUnknownValue = "talent.choice.unknown_value";
    public const string ReasonForbiddenSkillKind = "talent.choice.forbidden_skill_kind";
    public const string ReasonNotApplicable = "talent.choice.not_applicable";

    private static readonly IReadOnlyList<SkillKind> NonCombatNonMagic =
        [SkillKind.General, SkillKind.Knowledge, SkillKind.Social];

    /// <summary>Схема по bare-slug коду таланта; для остальных — <see cref="TalentChoiceSchema.None"/>.</summary>
    private static readonly Dictionary<string, TalentChoiceSchema> ByCode = new(StringComparer.Ordinal)
    {
        // На каждый ранг ровно одна ещё не выбранная этим талантом характеристика.
        ["povyshenie"] = new(TalentChoiceKind.Characteristic, 1, 1, DistinctAcrossRanks: true, []),
        // Ранг 1 — один навык, каждый следующий — два новых; только не боевые и не магические.
        ["kvalifikatsiya"] = new(TalentChoiceKind.Skill, 1, 2, DistinctAcrossRanks: true, NonCombatNonMagic),
        ["schastlivoe-popadanie"] = new(TalentChoiceKind.Characteristic, 1, 0, false, []),
        ["heroic-recovery"] = new(TalentChoiceKind.Characteristic, 1, 0, false, []),
        // Две разные характеристики за один (неранговый) талант.
        ["geroicheskaya-volya"] = new(TalentChoiceKind.Characteristic, 2, 0, DistinctAcrossRanks: true, []),
        ["odarennost"] = new(TalentChoiceKind.Skill, 2, 0, DistinctAcrossRanks: true, []),
        ["master"] = new(TalentChoiceKind.Skill, 1, 0, false, []),
        ["signature-spell"] = new(TalentChoiceKind.SpellConfiguration, 1, 0, false, []),
        ["zhivotnoe-sputnik"] = new(TalentChoiceKind.AnimalCompanion, 1, 1, false, []),
    };

    public static TalentChoiceSchema For(TalentDef definition) =>
        ByCode.GetValueOrDefault(TalentPurchasePolicy.BareCode(definition.Code), TalentChoiceSchema.None);

    /// <summary>Все таланты со схемой выбора — для тестов и генерации UI.</summary>
    public static IReadOnlyDictionary<string, TalentChoiceSchema> All => ByCode;

    /// <summary>
    /// Проверяет значения, присланные для покупаемого ранга.
    /// </summary>
    /// <param name="schema">Схема таланта.</param>
    /// <param name="rankIndex">Индекс покупаемого ранга (0 — первый).</param>
    /// <param name="values">Присланные стабильные значения.</param>
    /// <param name="alreadyChosen">Значения, выбранные этим талантом на прошлых рангах.</param>
    /// <param name="resolveSkillKind">Вид навыка по каноническому имени; <c>null</c> — навык неизвестен.</param>
    public static TalentChoiceError? Validate(
        TalentChoiceSchema schema,
        int rankIndex,
        IReadOnlyList<string> values,
        IReadOnlyCollection<string> alreadyChosen,
        Func<string, SkillKind?> resolveSkillKind)
    {
        var expected = schema.CountForRank(rankIndex);

        if (!schema.Required)
            return values.Count == 0
                ? null
                : new TalentChoiceError(ReasonNotApplicable, "Этот талант не требует выбора.");

        if (values.Count == 0 && expected > 0)
            return new TalentChoiceError(ReasonMissing,
                $"Для этого ранга нужно сделать выбор ({expected}).");

        if (values.Count != expected)
            return new TalentChoiceError(ReasonCount,
                $"Нужно выбрать ровно {expected} значений, получено {values.Count}.");

        if (values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            return new TalentChoiceError(ReasonDuplicate, "Значения выбора не должны повторяться.");

        if (schema.DistinctAcrossRanks)
        {
            var repeat = values.FirstOrDefault(v => alreadyChosen.Contains(v, StringComparer.Ordinal));
            if (repeat is not null)
                return new TalentChoiceError(ReasonDuplicate,
                    $"«{repeat}» уже выбрано этим талантом ранее.");
        }

        foreach (var value in values)
        {
            switch (schema.Kind)
            {
                case TalentChoiceKind.Characteristic:
                    if (!Enum.TryParse<CharacteristicType>(value, ignoreCase: true, out _))
                        return new TalentChoiceError(ReasonUnknownValue, $"Неизвестная характеристика «{value}».");
                    break;

                case TalentChoiceKind.Skill:
                    var kind = resolveSkillKind(value);
                    if (kind is null)
                        return new TalentChoiceError(ReasonUnknownValue, $"Навык «{value}» не найден.");
                    if (schema.AllowedSkillKinds.Count > 0 && !schema.AllowedSkillKinds.Contains(kind.Value))
                        return new TalentChoiceError(ReasonForbiddenSkillKind,
                            $"Навык «{value}» не подходит: нужен не боевой и не магический навык.");
                    break;

                case TalentChoiceKind.SpellConfiguration:
                case TalentChoiceKind.AnimalCompanion:
                    if (string.IsNullOrWhiteSpace(value))
                        return new TalentChoiceError(ReasonMissing, "Пустое значение выбора.");
                    break;
            }
        }

        return null;
    }
}
