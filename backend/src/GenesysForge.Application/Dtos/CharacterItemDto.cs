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

/// <summary>
/// Памятка по ремонту экземпляра (GEN-EQP-DMG-01): всё, что нужно знать до нажатия кнопки —
/// доступен ли обычный ремонт, какая по книге сложность, сколько это занимает времени и во что
/// обойдутся материалы. Считает сервер: стоимость идёт от цены экземпляра, а не строки каталога.
/// </summary>
/// <param name="MaterialCost">
/// Стоимость материалов, которую спишет кнопка ремонта. <c>null</c> — у записи нет обычной цены,
/// и сумму называет ведущий.
/// </param>
/// <param name="Affordable">Денег хватает: бюджет создания плюс кошелёк.</param>
public record ItemRepairDto(
    ItemDamageState State,
    bool CanRepair,
    int? Difficulty,
    int HoursMin,
    int HoursMax,
    int MaterialPercent,
    int? MaterialCost,
    string SkillName,
    bool Affordable);

/// <summary>
/// Магический инструмент на листе (ROT-MAG-IMP-01). Числа позиции — уже с материалом; здесь то,
/// что нужно сборщику заклинаний: какой эффект инструмент удешевляет и работает ли он вообще.
/// </summary>
/// <param name="Pending">
/// Экземпляр ещё не настроен ведущим: обычные числа у него есть, а бесплатный эффект не работает.
/// </param>
public record ItemImplementDto(
    string Code,
    ImplementMaterial Material,
    int AttackDamageBonus,
    int BoostDice,
    string RequiredMagicSkill,
    ImplementDiscountKind Discount,
    IReadOnlyList<string> DiscountEffects,
    int ChoiceCount,
    int? ChoiceMaxIncreaseSum,
    int? ChoiceExactIncrease,
    IReadOnlyList<string> ChosenEffects,
    bool Pending);

/// <summary>
/// Runebound shard на листе. Паспорт общий для каталога, а configuration принадлежит конкретному
/// экземпляру Lesser Rune.
/// </summary>
public record ItemRuneboundShardDto(
    RuneboundShardSpecDto Spec,
    string ActivationChoice,
    string EffectAction,
    string EffectChoice,
    bool Pending);

public record CharacterItemDto(Guid Id, Guid ItemDefId, string Name, string NameRu, ItemKind Kind, ItemState State, int Quantity,
    int Encumbrance, int SoakBonus, int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus, int Load,
    string Description, int? Price, string SkillName, string Damage, string Crit, string RangeBand, string Properties,
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
    int? Rarity = null,
    /// <summary>
    /// Экземпляр укреплён (Ancient): броня не поддаётся Pierce/Breach, а сам предмет — Sunder.
    /// </summary>
    bool Reinforced = false,
    /// <summary>Разбор поправок: что именно качество изготовления изменило и с какого значения.</summary>
    IReadOnlyList<ItemStatAdjustmentDto>? Adjustments = null,
    /// <summary>Установленные улучшения (ROT-EQP-ATT-01).</summary>
    IReadOnlyList<CharacterAttachmentDto>? Attachments = null,
    /// <summary>Занято слотов улучшений из <see cref="HardPoints"/>.</summary>
    int UsedHardPoints = 0,
    /// <summary>
    /// Улучшений стоит больше, чем осталось слотов: так бывает, когда слот отняла работа или
    /// новая редакция таблицы. Новые улучшения не ставятся, пока владелец не снимет лишнее.
    /// </summary>
    bool OverCapacity = false,
    /// <summary>
    /// Правила улучшений, которые приложение не исполняет: автоматические символы и эффекты,
    /// которым нужен рантайм столкновения. Показываются игроку, а не теряются.
    /// </summary>
    IReadOnlyList<string>? AttachmentNotes = null,
    /// <summary>Признаки формы: по ним считается совместимость улучшений (ROT-EQP-ATT-01).</summary>
    WeaponFormTraits FormTraits = WeaponFormTraits.None,
    /// <summary>
    /// Состояние повреждения экземпляра (GEN-EQP-DMG-01). Числа позиции выше — уже с его учётом:
    /// у серьёзно повреждённого предмета поглощение, защита и прибавка к порогу веса обнулены.
    /// </summary>
    ItemDamageState DamageState = ItemDamageState.Undamaged,
    /// <summary>
    /// Предметом можно пользоваться. <c>false</c> — Серьёзное повреждение или Уничтожено: атаки,
    /// поглощение, защита и эффекты улучшений не действуют, но вес и содержимое остаются.
    /// </summary>
    bool IsUsable = true,
    /// <summary>Памятка по ремонту: сложность, время, доля и стоимость материалов.</summary>
    ItemRepairDto? Repair = null,
    /// <summary>
    /// Магический инструмент (ROT-MAG-IMP-01); <c>null</c> — запись инструментом не является.
    /// </summary>
    ItemImplementDto? Implement = null,
    /// <summary>Runebound shard instance; <c>null</c> у обычного предмета.</summary>
    ItemRuneboundShardDto? Shard = null,
    bool Sellable = true);
