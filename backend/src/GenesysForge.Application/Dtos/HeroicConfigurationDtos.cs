using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Запрос на выбор параметра primary effect (ROT-HA-02). Заполняется только релевантная часть:
/// поля чужого параметра отклоняются, а не игнорируются молча.
/// </summary>
public record SetHeroicConfigurationRequest(
    Guid? ParagonSkillDefId,
    string? SixthSenseSubject,
    SignatureWeaponProfile? WeaponProfile,
    WeaponCraftsmanship? Craftsmanship,
    string? NarrativeForm,
    WeaponFormTraits? FormTraits);

/// <summary>Заменa или возврат потерянного именного оружия (GM/narrative команда).</summary>
public record ReplaceSignatureWeaponRequest(
    bool Lost,
    SignatureWeaponProfile? WeaponProfile,
    WeaponCraftsmanship? Craftsmanship,
    string? NarrativeForm,
    WeaponFormTraits? FormTraits);

/// <summary>Полный профиль именного оружия: числа строит сервер из выбранного профиля.</summary>
public record SignatureWeaponDto(
    SignatureWeaponProfile Profile,
    WeaponCraftsmanship Craftsmanship,
    string NarrativeForm,
    WeaponFormTraits FormTraits,
    bool IsLost,
    string SkillName,
    string Damage,
    int Crit,
    string RangeBand,
    int Encumbrance,
    int HardPoints,
    List<ItemQualityRefDto> Qualities);

/// <summary>Параметр primary effect на листе персонажа.</summary>
/// <param name="Kind">Какой параметр требуется выбранной способностью.</param>
/// <param name="ParagonSkillMissing">
/// Навык Paragon больше не виден персонажу (например, кастомный навык скрыт). Снимок имени
/// сохранён; другой навык вместо него не подставляется.
/// </param>
public record HeroicConfigurationDto(
    HeroicParameterKind Kind,
    Guid? ParagonSkillDefId,
    string? ParagonSkillName,
    bool ParagonSkillMissing,
    string? SixthSenseSubject,
    SignatureWeaponDto? SignatureWeapon,
    bool Complete);
