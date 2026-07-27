using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Одна поправка к характеристике экземпляра (ROT-WPN-02): что изменилось, с чего на что, на каком
/// этапе и от чего. Клиент показывает разбор, а готовые числа берёт из полей позиции.
/// </summary>
/// <param name="Field">Стабильный код характеристики: <c>encumbrance</c>, <c>soak</c>, <c>price</c>…</param>
public record ItemStatAdjustmentDto(
    string Field, int Base, int Effective, ItemStatStage Stage, string Source);

public record CharacterItemDto(Guid Id, Guid ItemDefId, string Name, string NameRu, ItemKind Kind, ItemState State, int Quantity,
    int Encumbrance, int SoakBonus, int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus, int Load,
    string Description, int Price, string SkillName, string Damage, string Crit, string RangeBand, string Properties,
    string DescriptionEn = "",
    /// <summary>
    /// Позиция выбрана активной бронёй (ROT-CMB-02): только она даёт защиту и поглощение.
    /// Прочая надетая броня продолжает считаться в переносимый вес.
    /// </summary>
    bool IsActiveArmor = false,
    /// <summary>
    /// Слоты улучшений по таблице книги (ROT-WPN-01/ROT-ARM-01); <c>null</c> — значения нет.
    /// </summary>
    int? HardPoints = null,
    /// <summary>Влияние предмета на проверки навыков: штраф Скрытности у тяжёлой брони и т. п.</summary>
    IReadOnlyList<ItemCheckModifierDto>? CheckModifiers = null,
    /// <summary>
    /// Профили атаки с уже посчитанным для этого персонажа базовым уроном (ROT-WPN-01):
    /// основной и альтернативные (метание, в руке).
    /// </summary>
    IReadOnlyList<WeaponAttackProfileDto>? AttackProfiles = null,
    /// <summary>
    /// Оружие метнули и не подобрали: атаковать им нельзя, качеств и веса оно не даёт,
    /// но и не исчезает.
    /// </summary>
    bool IsThrown = false,
    /// <summary>
    /// Качество изготовления экземпляра (ROT-WPN-02). Числа позиции выше — уже с его поправками:
    /// вес, поглощение, защита, слоты, цена, редкость и профили атаки.
    /// </summary>
    WeaponCraftsmanship Craftsmanship = WeaponCraftsmanship.Steel,
    /// <summary>Редкость экземпляра: Ancient задаёт ровно десять, остальные типы сдвигают каталожную.</summary>
    int Rarity = 0,
    /// <summary>
    /// Экземпляр укреплён (Ancient): броня не поддаётся Pierce/Breach, а сам предмет — Sunder.
    /// </summary>
    bool Reinforced = false,
    /// <summary>Разбор поправок: что именно качество изготовления изменило и с какого значения.</summary>
    IReadOnlyList<ItemStatAdjustmentDto>? Adjustments = null);
