namespace GenesysForge.Domain.Rules;

/// <summary>
/// Личность героической способности (ROT-HA-01): личное название игрока и происхождение.
/// Это три разных понятия — primary effect из каталога, личное название и origin, — поэтому
/// название никогда не подменяется отображаемым именем эффекта.
/// </summary>
/// <param name="CustomName">Личное название способности, заданное игроком.</param>
/// <param name="OriginMode">Как задано происхождение.</param>
/// <param name="OriginPrimary">Первая категория таблицы; <c>null</c> только в режиме Custom.</param>
/// <param name="OriginSecondary">Вторая категория; заполнена только в режиме DoubleStandard.</param>
/// <param name="OriginNarrative">Собственный текст: обязателен для Custom, допустим как заметка к категориям.</param>
/// <param name="OriginRolls">Фактические грани d10, если происхождение было брошено, иначе пусто.</param>
public sealed record HeroicIdentity(
    string CustomName,
    HeroicOriginMode OriginMode,
    HeroicOriginType? OriginPrimary,
    HeroicOriginType? OriginSecondary,
    string? OriginNarrative,
    IReadOnlyList<int> OriginRolls);

/// <summary>
/// Правила заполнения и проверки <see cref="HeroicIdentity"/>. Валидация выполняется целиком
/// до первой мутации, а отказ несёт машинный <c>reasonCode</c> — клиент ветвится по нему,
/// а не по тексту сообщения.
/// </summary>
public static class HeroicIdentityRules
{
    public const int NameMaxLength = 120;
    public const int NarrativeMaxLength = 2000;

    /// <summary>
    /// Полнота личности без выбрасывания исключения: используется листом, гейтами покупок и
    /// импортом, где неполные legacy-данные допустимы, но должны быть видимы.
    /// </summary>
    public static bool IsComplete(
        string? customName,
        HeroicOriginMode? mode,
        HeroicOriginType? primary,
        HeroicOriginType? secondary,
        string? narrative)
    {
        if (string.IsNullOrWhiteSpace(customName) || mode is null) return false;
        return mode switch
        {
            HeroicOriginMode.Standard => primary is not null,
            HeroicOriginMode.DoubleStandard => primary is not null && secondary is not null,
            HeroicOriginMode.Custom => !string.IsNullOrWhiteSpace(narrative),
            _ => false,
        };
    }

    /// <summary>
    /// Проверяет и нормализует данные личности. Пустые и слишком длинные значения, а также
    /// несовместимые с режимом поля отклоняются: молча обрезать текст или достраивать
    /// происхождение нельзя.
    /// </summary>
    public static HeroicIdentity Validate(
        string? customName,
        HeroicOriginMode mode,
        HeroicOriginType? primary,
        HeroicOriginType? secondary,
        string? narrative,
        IReadOnlyList<int>? rolls = null)
    {
        var name = customName?.Trim() ?? "";
        if (name.Length == 0)
            throw new DomainRuleException(
                "Укажите личное название героической способности.", "heroic.identity.name_required");
        if (name.Length > NameMaxLength)
            throw new DomainRuleException(
                $"Личное название героической способности длиннее {NameMaxLength} символов.",
                "heroic.identity.name_too_long");

        if (!Enum.IsDefined(mode))
            throw new DomainRuleException(
                "Неизвестный способ задания происхождения.", "heroic.identity.origin_mode_unknown");

        var text = string.IsNullOrWhiteSpace(narrative) ? null : narrative.Trim();
        if (text is not null && text.Length > NarrativeMaxLength)
            throw new DomainRuleException(
                $"Описание происхождения длиннее {NarrativeMaxLength} символов.",
                "heroic.identity.narrative_too_long");

        RequireDefined(primary, "heroic.identity.origin_unknown");
        RequireDefined(secondary, "heroic.identity.origin_unknown");

        switch (mode)
        {
            case HeroicOriginMode.Standard:
                if (primary is null)
                    throw new DomainRuleException(
                        "Выберите категорию происхождения или бросьте по таблице.",
                        "heroic.identity.origin_required");
                if (secondary is not null)
                    throw new DomainRuleException(
                        "Вторая категория происхождения появляется только после специального результата броска.",
                        "heroic.identity.origin_second_not_allowed");
                break;

            case HeroicOriginMode.DoubleStandard:
                if (primary is null || secondary is null)
                    throw new DomainRuleException(
                        "Специальный результат таблицы даёт ровно две категории происхождения.",
                        "heroic.identity.origin_second_required");
                break;

            case HeroicOriginMode.Custom:
                if (primary is not null || secondary is not null)
                    throw new DomainRuleException(
                        "Собственное происхождение не совмещается с категориями таблицы.",
                        "heroic.identity.origin_not_allowed");
                if (text is null)
                    throw new DomainRuleException(
                        "Опишите собственное происхождение способности.",
                        "heroic.identity.narrative_required");
                break;
        }

        return new HeroicIdentity(name, mode, primary, secondary, text, rolls ?? []);
    }

    /// <summary>Сериализация фактических граней для хранения и истории: «0,0,4,7».</summary>
    public static string FormatRolls(IReadOnlyList<int>? rolls) =>
        rolls is null || rolls.Count == 0 ? "" : string.Join(",", rolls);

    /// <summary>
    /// Разбор сохранённых граней. Повреждённая строка даёт пустой список: грани — данные аудита,
    /// восстанавливать их догадкой нельзя, но и ломать из-за них загрузку персонажа не нужно.
    /// </summary>
    public static IReadOnlyList<int> ParseRolls(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<int>(parts.Length);
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var face) || face is < 0 or > 9) return [];
            result.Add(face);
        }
        return result;
    }

    private static void RequireDefined(HeroicOriginType? type, string reasonCode)
    {
        if (type is not null && !Enum.IsDefined(type.Value))
            throw new DomainRuleException("Неизвестная категория происхождения.", reasonCode);
    }
}
