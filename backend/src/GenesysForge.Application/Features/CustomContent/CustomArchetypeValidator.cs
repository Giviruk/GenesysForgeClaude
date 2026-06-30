using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.CustomContent;

internal static class CustomArchetypeValidator
{
    public static void Validate(CreateCustomArchetypeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название архетипа не может быть пустым.");

        foreach (var (value, label) in Characteristics(req))
            if (value is < 1 or > 5)
                throw new DomainRuleException($"{label} должна быть от 1 до 5.");

        if (req.WoundBase is < 1 or > 30)
            throw new DomainRuleException("Базовый порог ран должен быть от 1 до 30.");
        if (req.StrainBase is < 1 or > 30)
            throw new DomainRuleException("Базовый порог стрейна должен быть от 1 до 30.");
        if (req.StartingXp is < 0 or > 500)
            throw new DomainRuleException("Стартовый XP должен быть от 0 до 500.");
    }

    private static IEnumerable<(int Value, string Label)> Characteristics(CreateCustomArchetypeRequest req)
    {
        yield return (req.Brawn, "Brawn");
        yield return (req.Agility, "Agility");
        yield return (req.Intellect, "Intellect");
        yield return (req.Cunning, "Cunning");
        yield return (req.Willpower, "Willpower");
        yield return (req.Presence, "Presence");
    }
}
