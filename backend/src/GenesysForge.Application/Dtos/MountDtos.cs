using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Профиль скакуна из справочника (ROT-MOUNT-ITEM-01). Числа приходят структурно, поэтому витрина
/// и лист показывают статблок, а не строку «Снаряжение».
/// </summary>
/// <param name="Price">Цена; <c>null</c> — бесценно, обычная покупка недоступна.</param>
/// <param name="Capacity">Вместимость профиля: она приоритетнее общего правила <c>5 + Сила</c>.</param>
public record MountDefDto(
    Guid Id,
    string Code,
    string Name,
    string NameRu,
    NpcKind Kind,
    Dictionary<string, int> Characteristics,
    int Soak,
    int WoundThreshold,
    int? StrainThreshold,
    int MeleeDefense,
    int RangedDefense,
    int Silhouette,
    int Capacity,
    int? Price,
    int Rarity,
    IReadOnlyList<string> IncludedGear,
    bool RequiresRidingCheck,
    IReadOnlyList<MountSkillDto> Skills,
    IReadOnlyList<MountAbilityDto> Abilities,
    IReadOnlyList<MountAttackDto> Attacks,
    string Description,
    string DescriptionEn,
    string Source);

/// <param name="IsGroupSkill">Групповой навык Minion: ранг даёт группа, а не запись.</param>
public record MountSkillDto(string Name, int Ranks, bool IsGroupSkill);

public record MountAbilityDto(string Name, string NameRu, string Description, string DescriptionEn);

public record MountAttackDto(
    string Name,
    string NameRu,
    string SkillName,
    int Damage,
    int Critical,
    WeaponRange Range,
    IReadOnlyList<string> QualityCodes);

/// <summary>
/// Скакун персонажа. Собственного веса у него нет: <c>CarriedLoad</c> — груз самого скакуна, и в
/// Encumbrance владельца он не входит.
/// </summary>
/// <param name="DisplayName">Кличка, если задана, иначе название профиля.</param>
/// <param name="Capacity">Вместимость профиля (приоритетнее <c>5 + Сила</c>).</param>
/// <param name="IsOverloaded">Груза больше вместимости.</param>
/// <param name="IsIncapacitated">Раны достигли порога профиля — скакун выведен из строя.</param>
public record CharacterMountDto(
    Guid Id,
    Guid MountDefId,
    string DisplayName,
    string Name,
    MountDefDto Definition,
    int WoundsCurrent,
    int CarriedLoad,
    int Capacity,
    bool IsActive,
    bool IsOverloaded,
    bool IsIncapacitated,
    ItemProvenance Provenance,
    string Notes);

/// <summary>
/// Покупка или бесплатная выдача скакуна. Сумму считает сервер по цене каталога (ROT-ECO-01):
/// клиент выбирает только профиль и способ оплаты.
/// </summary>
/// <param name="Free">Скакуна выдал ведущий — кошелёк не трогается.</param>
/// <param name="PriceOverride">
/// Договорная цена, назначенная вместо каталожной. Требует <paramref name="OverrideReason"/> и
/// целиком попадает в историю персонажа.
/// </param>
/// <param name="PricePercent">
/// Доля цены при торге: 50…200 % с шагом 25, как у предметов. Взаимоисключающа с
/// <paramref name="PriceOverride"/>.
/// </param>
public record BuyMountRequest(
    Guid MountDefId,
    bool Free = false,
    int? PriceOverride = null,
    string? OverrideReason = null,
    int? PricePercent = null,
    string? Name = null);

/// <summary>
/// Продажа скакуна. Способы те же три, что у предметов (ROT-ECO-01), и сумму во всех случаях
/// считает сервер.
/// </summary>
/// <param name="NetSuccesses">Нетто-успехи проверки: 1 успех — 25 %, 2 — 50 %, 3 и больше — 75 %.</param>
/// <param name="Percent">Доля цены каталога, 0–100, для продажи без проверки.</param>
/// <param name="PriceOverride">Договорная цена; требует <paramref name="OverrideReason"/>.</param>
public record SellMountRequest(
    int? NetSuccesses = null,
    int? Percent = null,
    int? PriceOverride = null,
    string? OverrideReason = null,
    double? ConditionMultiplier = null,
    string? ConditionReason = null);

/// <summary>
/// Правка состояния скакуна: кличка, раны, груз, «под седлом», заметка. Все поля необязательные —
/// присланные меняются, остальные остаются как были.
/// </summary>
public record UpdateMountRequest(
    string? Name = null,
    int? WoundsCurrent = null,
    int? CarriedLoad = null,
    bool? IsActive = null,
    string? Notes = null);
