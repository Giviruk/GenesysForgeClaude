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
    WeaponFormTraits? FormTraits,
    /// <summary>Базовое улучшение оружия: временное, бесплатное и без слотов (ROT-HA-02).</summary>
    Guid? BaseAttachmentDefId = null);

/// <summary>Заменa или возврат потерянного именного оружия (GM/narrative команда).</summary>
public record ReplaceSignatureWeaponRequest(
    bool Lost,
    SignatureWeaponProfile? WeaponProfile,
    WeaponCraftsmanship? Craftsmanship,
    string? NarrativeForm,
    WeaponFormTraits? FormTraits,
    Guid? BaseAttachmentDefId = null);

/// <summary>Полный профиль именного оружия: числа строит сервер из выбранного профиля.</summary>
/// <param name="Damage">Урон профиля вместе с вкладом базового улучшения.</param>
/// <param name="Qualities">Качества профиля вместе с теми, что даёт базовое улучшение.</param>
/// <param name="BaseAttachment">
/// Базовое улучшение (ROT-HA-02): временное, действует только вместе со способностью,
/// не занимает слотов и не покупается. <c>null</c> у старого персонажа, который его ещё не выбрал.
/// </param>
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
    List<ItemQualityRefDto> Qualities,
    SignatureBaseAttachmentDto? BaseAttachment = null);

/// <summary>
/// Базовое улучшение именного оружия. Цена и слоты не приводятся намеренно: у героической копии
/// их нет, и показывать «стоит 1250, занимает 1 HP» было бы неправдой.
/// </summary>
public record SignatureBaseAttachmentDto(
    Guid DefId,
    string Code,
    string Name,
    string NameRu,
    string Description,
    IReadOnlyList<AttachmentEffectDto> Effects);

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
