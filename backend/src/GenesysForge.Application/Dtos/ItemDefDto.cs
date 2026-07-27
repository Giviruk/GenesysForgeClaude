using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record ItemDefDto(Guid Id, string Name, string NameRu, ItemKind Kind, int Encumbrance, int SoakBonus,
    int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus,
    string Description, string SafeDescription, string Source, int Price, int Rarity,
    string SkillName, string Damage, string Crit, string RangeBand, string Properties, bool IsCustom,
    IReadOnlyList<ItemQualityRefDto> Qualities, string DescriptionEn = "",
    /// <summary>
    /// Слоты улучшений по таблице книги; <c>null</c> — книжного значения у записи нет
    /// (ROT-WPN-01/ROT-ARM-01). Ноль означает «улучшения ставить некуда».
    /// </summary>
    int? HardPoints = null,
    /// <summary>Влияние предмета на проверки навыков (ROT-ARM-01).</summary>
    IReadOnlyList<ItemCheckModifierDto>? CheckModifiers = null,
    /// <summary>Типизированные профили атаки (ROT-WPN-01); пусто у не-оружия.</summary>
    IReadOnlyList<WeaponAttackProfileDto>? AttackProfiles = null);

/// <summary>Штраф или послабление предмета к проверкам конкретного навыка/характеристики.</summary>
public record ItemCheckModifierDto(
    CheckModifierKind Kind, string SkillName, CharacteristicType? Characteristic, int Value,
    bool RequiresWorn, string Condition);

/// <summary>
/// Профиль атаки оружия (ROT-WPN-01). Урон разложен на тип и значение, поэтому клиент больше не
/// разбирает строку «+3»; <paramref name="BaseDamage"/> уже посчитан для текущей Мощи персонажа
/// там, где профиль отдаётся с листа.
/// </summary>
public record WeaponAttackProfileDto(
    string Code, string NameRu, string NameEn, bool IsDefault,
    string SkillName, DamageKind DamageKind, int DamageValue, int Crit, WeaponRange Range,
    bool CannotAttackEngaged, int? FixedDifficulty,
    IReadOnlyList<ItemQualityRefDto> Qualities,
    int? BaseDamage = null);

/// <summary>Структурное качество предмета: ссылка на справочник (по коду) + рейтинг.</summary>
public record ItemQualityRefDto(
    string Code, string NameRu, string NameEn, int? Rating, bool HasRating, bool IsActive, string ActivationCost);
