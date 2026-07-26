using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

/// <summary>Доступ к параметрам primary effect (ROT-HA-02) — те же условия, что и у личности.</summary>
public static class HeroicConfigurationGate
{
    /// <summary>
    /// Параметр выбирается во время создания и потом неизменяем; исключение — однократное
    /// заполнение старого персонажа, у которого параметра ещё нет.
    /// </summary>
    public static void EnsureEditable(Character c)
    {
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException(
                "Героические способности доступны только в Realms of Terrinoth.", "heroic.system_not_supported");
        if (c.HeroicAbilityId is null)
            throw new DomainRuleException(
                "Сначала выберите героическую способность.", "heroic.ability.required");
        if (!c.IsCreationPhase && !c.HeroicConfigurationIncomplete)
            throw new DomainRuleException(
                "Параметр героической способности после завершения создания не меняется.",
                "heroic.parameter.immutable");
    }

    /// <summary>Параметр обязан быть выбран — проверка перед завершением создания.</summary>
    public static void EnsureComplete(Character c)
    {
        if (c.HeroicConfigurationIncomplete)
            throw new DomainRuleException(
                "Выберите параметр героической способности до завершения создания.",
                "heroic.parameter.incomplete");
    }

    /// <summary>Улучшения недоступны завершённому персонажу с невыбранным параметром.</summary>
    public static void EnsureUpgradesAllowed(Character c)
    {
        if (!c.IsCreationPhase && c.HeroicConfigurationIncomplete)
            throw new DomainRuleException(
                "Выберите параметр героической способности — после этого улучшения станут доступны.",
                "heroic.parameter.incomplete");
    }
}
