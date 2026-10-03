namespace GenesysForge.Domain.Rules;

/// <summary>
/// Конфигурация таланта Signature Spell (ROT-TAL-03): одно магическое действие и непустой
/// мультисет его дополнительных эффектов. Хранится строкой «Действие|Эффект|Эффект…» по стабильным
/// кодам (<c>NameEn</c> справочника магии). Эффекты сортируются: порядок выбора не важен,
/// кратность важна.
/// </summary>
public sealed record SignatureSpellConfiguration(string Action, IReadOnlyList<string> Effects)
{
    public const char Separator = '|';

    /// <summary>Разбирает сохранённое значение; <c>null</c> — нет действия или ни одного эффекта.</summary>
    public static SignatureSpellConfiguration? Parse(string value)
    {
        var parts = value.Split(Separator, StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts.Any(part => part.Length == 0)) return null;
        return new SignatureSpellConfiguration(parts[0], [.. parts.Skip(1).Order(StringComparer.Ordinal)]);
    }

    /// <summary>Каноническая строка: одинаковые конфигурации дают одно и то же значение.</summary>
    public string Format() =>
        string.Join(Separator, Effects.Order(StringComparer.Ordinal).Prepend(Action));
}

/// <summary>Дополнительный эффект справочника магии в том объёме, что нужен для проверки.</summary>
/// <param name="Code">Стабильный код (<c>NameEn</c>).</param>
/// <param name="Repeatable">Эффект можно добавить к заклинанию несколько раз.</param>
/// <param name="Exclusions">Коды эффектов, с которыми этот не сочетается.</param>
public sealed record SignatureSpellEffect(string Code, bool Repeatable, IReadOnlyList<string> Exclusions);

/// <summary>Проверка конфигурации Signature Spell по справочнику магии до списания XP.</summary>
public static class SignatureSpellRules
{
    /// <param name="configuration">Разобранная конфигурация.</param>
    /// <param name="actionExists">Есть ли такое магическое действие в системе персонажа.</param>
    /// <param name="resolveEffect">
    /// Дополнительный эффект по коду действия и коду эффекта; <c>null</c> — у действия такого нет.
    /// </param>
    public static TalentChoiceError? Validate(
        SignatureSpellConfiguration configuration,
        Func<string, bool> actionExists,
        Func<string, string, SignatureSpellEffect?> resolveEffect)
    {
        if (!actionExists(configuration.Action))
            return new TalentChoiceError(TalentChoiceSchemas.ReasonUnknownValue,
                $"Магическое действие «{configuration.Action}» не найдено.");

        var effects = new List<SignatureSpellEffect>();
        foreach (var code in configuration.Effects.Distinct(StringComparer.Ordinal))
        {
            var effect = resolveEffect(configuration.Action, code);
            if (effect is null)
                return new TalentChoiceError(TalentChoiceSchemas.ReasonUnknownValue,
                    $"Эффект «{code}» недоступен действию «{configuration.Action}».");
            if (!effect.Repeatable
                && configuration.Effects.Count(x => string.Equals(x, code, StringComparison.Ordinal)) > 1)
                return new TalentChoiceError(TalentChoiceSchemas.ReasonDuplicate,
                    $"Эффект «{code}» нельзя добавить к заклинанию дважды.");
            effects.Add(effect);
        }

        var chosen = effects.Select(e => e.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var effect in effects)
        {
            var conflict = effect.Exclusions.FirstOrDefault(code =>
                code != effect.Code && chosen.Contains(code));
            if (conflict is not null)
                return new TalentChoiceError(TalentChoiceSchemas.ReasonSpellConflict,
                    $"Эффекты «{effect.Code}» и «{conflict}» не сочетаются.");
        }

        return null;
    }
}
