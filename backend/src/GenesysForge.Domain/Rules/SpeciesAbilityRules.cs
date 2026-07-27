using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Пассивные видовые эффекты, влияющие на лист (ROT-SPECIES-01). Активируемые способности
/// (Story Point, счётчики применений, метки целей) живут в сессии и здесь не считаются.
/// Все правила выбираются по <see cref="ArchetypeAbilityDef.RuleKind"/>, а не по имени.
/// </summary>
public static class SpeciesAbilityRules
{
    /// <summary>
    /// Базовая защита, задаваемая видом (Nimble). Это provider/set, а не «+1»: с бронёй,
    /// дающей Defense 1, итог остаётся 1, пока не появятся настоящие additive-модификаторы.
    /// <c>null</c> — вид базовую защиту не задаёт.
    /// </summary>
    public static int? BaseDefense(IEnumerable<ArchetypeAbilityDef> abilities) => abilities
        .Where(a => a.RuleKind == SpeciesAbilityRuleKind.SetBaseDefense)
        .Select(a => (int?)a.RuleValue)
        .DefaultIfEmpty(null)
        .Max();

    /// <summary>
    /// Итоговый silhouette: базовое значение вида, перекрытое способностью <c>Small</c>, если она есть.
    /// </summary>
    public static int Silhouette(ArchetypeDef archetype) => archetype.Abilities
        .Where(a => a.RuleKind == SpeciesAbilityRuleKind.SetSilhouette)
        .Select(a => a.RuleValue)
        .DefaultIfEmpty(archetype.Silhouette)
        .Min();

    /// <summary>
    /// Способности вида, действующие для конкретного персонажа. У вида с обязательным выбором
    /// (Half-Catfolk) возвращается только выбранная опция; пока выбор не сделан — ни одной,
    /// потому что подставлять её автоматически запрещено.
    /// </summary>
    public static IEnumerable<ArchetypeAbilityDef> EffectiveAbilities(
        ArchetypeDef archetype, string? chosenCode, IReadOnlyDictionary<string, ArchetypeAbilityDef> byCode)
    {
        foreach (var ability in archetype.Abilities)
        {
            if (ability.RuleKind != SpeciesAbilityRuleKind.ChooseOneAbility)
            {
                yield return ability;
                continue;
            }

            if (string.IsNullOrWhiteSpace(chosenCode)) continue;
            if (!ChoiceOptions(ability).Contains(chosenCode, StringComparer.Ordinal)) continue;
            if (byCode.TryGetValue(chosenCode, out var chosen)) yield return chosen;
        }
    }

    /// <summary>Допустимые коды для способности-выбора, в объявленном порядке.</summary>
    public static IReadOnlyList<string> ChoiceOptions(ArchetypeAbilityDef ability) =>
        RuleParameter(ability, "options") is { Length: > 0 } list
            ? list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>
    /// Значение именованного параметра из <see cref="ArchetypeAbilityDef.RuleParameters"/>
    /// формата <c>key=value;key2=value2</c>. Пусто — параметра нет.
    /// </summary>
    public static string RuleParameter(ArchetypeAbilityDef ability, string key)
    {
        foreach (var part in ability.RuleParameters.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0) continue;
            if (part.AsSpan(0, separator).Trim().SequenceEqual(key))
                return part[(separator + 1)..].Trim();
        }
        return "";
    }

    /// <summary>Требуется ли персонажу сделать обязательный видовой выбор, который ещё не сделан.</summary>
    public static bool ChoiceIncomplete(ArchetypeDef archetype, string? chosenCode) =>
        archetype.Abilities.Any(a => a.RuleKind == SpeciesAbilityRuleKind.ChooseOneAbility)
        && string.IsNullOrWhiteSpace(chosenCode);
}
