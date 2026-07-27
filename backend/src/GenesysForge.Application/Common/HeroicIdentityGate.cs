using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

/// <summary>
/// Общие проверки доступа к личности героической способности (ROT-HA-01). Держатся отдельно,
/// потому что одни и те же условия нужны и командам заполнения, и гейтам покупки улучшений.
/// </summary>
public static class HeroicIdentityGate
{
    /// <summary>
    /// Личность заполняется во время создания и после него неизменяема. Единственное исключение —
    /// однократный ремонт старого персонажа, у которого данных ещё нет: как только они заполнены,
    /// запись снова закрывается.
    /// </summary>
    public static void EnsureEditable(Character c)
    {
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException(
                "Героические способности доступны только в Realms of Terrinoth.", "heroic.system_not_supported");
        if (c.HeroicAbilityId is null)
            throw new DomainRuleException(
                "Сначала выберите героическую способность.", "heroic.ability.required");
        if (!c.IsCreationPhase && !c.HeroicIdentityIncomplete)
            throw new DomainRuleException(
                "Личное название и происхождение героической способности после завершения создания не меняются.",
                "heroic.identity.immutable");
    }

    /// <summary>Личность обязана быть заполнена целиком — проверка перед завершением создания.</summary>
    public static void EnsureComplete(Character c)
    {
        if (c.HeroicIdentityIncomplete)
            throw new DomainRuleException(
                "Укажите личное название и происхождение героической способности до завершения создания.",
                "heroic.identity.incomplete");
    }

    /// <summary>
    /// Улучшения недоступны завершённому персонажу с незаполненной личностью: это legacy-данные,
    /// и покупка закрепилась бы навсегда на способности, происхождение которой никто не выбирал.
    /// В фазе создания ограничения нет — там личность всё равно требует завершение создания.
    /// </summary>
    public static void EnsureUpgradesAllowed(Character c)
    {
        if (!c.IsCreationPhase && c.HeroicIdentityIncomplete)
            throw new DomainRuleException(
                "Заполните личное название и происхождение героической способности — после этого улучшения станут доступны.",
                "heroic.identity.incomplete");
    }
}
