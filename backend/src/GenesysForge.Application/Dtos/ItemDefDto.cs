using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Паспорт магического инструмента в справочнике (ROT-MAG-IMP-01): по нему витрина показывает
/// выбор материала, а сборщик заклинаний — что именно инструмент удешевляет.
/// </summary>
public record ImplementSpecDto(
    string Code,
    int AttackDamageBonus,
    int BoostDice,
    string RequiredMagicSkill,
    ImplementDiscountKind Discount,
    IReadOnlyList<string> DiscountEffects,
    int ChoiceCount,
    int? ChoiceMaxIncreaseSum,
    int? ChoiceExactIncrease);

public record ShardSpellEffectDto(
    string Action,
    string EffectCode,
    ShardSpellEffectMode Mode,
    int FreeUses,
    bool OverridesSkillRestriction);

public record ShardDifficultyReductionDto(string Action, int Amount);

public record ShardActivationQualityDto(string Code, int? Rating);

public record ShardActivationAttackDto(
    string Skill,
    int Damage,
    int Critical,
    string Range,
    IReadOnlyList<ShardActivationQualityDto> Qualities);

/// <summary>Структурный паспорт runebound shard для справочника и Magic Builder.</summary>
public record RuneboundShardSpecDto(
    string Code,
    string RequiredMagicSkill,
    int MinimumSkillRank,
    int AttackDamageBonus,
    int CastingStrainReduction,
    IReadOnlyList<ShardDifficultyReductionDto> DifficultyReductions,
    IReadOnlyList<ShardSpellEffectDto> SpellEffects,
    ShardActivationCost ActivationCost,
    string ActivationFrequency,
    ShardActivationAttackDto? ActivationAttack,
    bool NeedsConfiguration);

public record ItemDefDto(Guid Id, string Name, string NameRu, ItemKind Kind, int Encumbrance, int SoakBonus,
    int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus,
    string Description, string SafeDescription, string Source, int? Price, int? Rarity,
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
    IReadOnlyList<WeaponAttackProfileDto>? AttackProfiles = null,
    /// <summary>
    /// Магический инструмент (ROT-MAG-IMP-01); <c>null</c> — запись инструментом не является.
    /// По нему витрина показывает выбор материала, а сборщик заклинаний — скидку на сложность.
    /// </summary>
    ImplementSpecDto? Implement = null,
    /// <summary>Runebound shard и его структурная implement-механика (ROT-MAG-11).</summary>
    RuneboundShardSpecDto? Shard = null,
    bool Purchasable = true,
    bool Sellable = true);

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
    int? BaseDamage = null,
    /// <summary>
    /// Что качества профиля делают с пулом атаки (GEN-EQP-QUAL-01). Заполняется там, где известны
    /// характеристики персонажа; в справочнике <c>null</c>.
    /// </summary>
    AttackPoolModifiersDto? PoolModifiers = null);

/// <summary>
/// Изменение пула атаки от качеств оружия. Автоматические преимущества и угрозы кубами не
/// являются — они прибавляются к результату броска, поэтому едут отдельными полями.
/// </summary>
public record AttackPoolModifiersDto(
    int Boost, int Setback, int DifficultyIncrease, int AutomaticAdvantage, int AutomaticThreat,
    IReadOnlyList<QualityContributionDto> Sources);

/// <summary>Вклад одного качества в пул — чтобы игрок видел, откуда взялся куб.</summary>
public record QualityContributionDto(
    string NameEn, string NameRu, int Boost, int Setback, int Difficulty, int Advantage, int Threat);

/// <summary>Структурное качество предмета: ссылка на справочник (по коду) + рейтинг.</summary>
public record ItemQualityRefDto(
    string Code, string NameRu, string NameEn, int? Rating, bool HasRating, bool IsActive, string ActivationCost);
