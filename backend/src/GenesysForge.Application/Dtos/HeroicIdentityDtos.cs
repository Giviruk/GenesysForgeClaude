using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Запрос на заполнение личности героической способности (ROT-HA-01).
/// <paramref name="OriginMode"/> <c>null</c> — «оставить уже сохранённое происхождение»
/// (например, только что бросок по таблице) и изменить одно личное название.
/// </summary>
public record SetHeroicIdentityRequest(
    string? CustomName,
    HeroicOriginMode? OriginMode,
    HeroicOriginType? OriginPrimary,
    HeroicOriginType? OriginSecondary,
    string? OriginNarrative);

/// <summary>Личность героической способности на листе персонажа.</summary>
/// <param name="Complete">Все обязательные части заполнены; иначе улучшения заблокированы.</param>
/// <param name="OriginRolls">Фактические грани броска, «0» — специальный результат. Пусто, если происхождение выбрано вручную.</param>
public record HeroicIdentityDto(
    string? CustomName,
    HeroicOriginMode? OriginMode,
    HeroicOriginType? OriginPrimary,
    HeroicOriginType? OriginSecondary,
    string? OriginNarrative,
    List<int> OriginRolls,
    bool Complete);

/// <summary>Результат броска по таблице происхождения: и грани, и разрешённые категории.</summary>
public record HeroicOriginRollDto(
    List<int> Rolls,
    HeroicOriginMode OriginMode,
    HeroicOriginType OriginPrimary,
    HeroicOriginType? OriginSecondary);
