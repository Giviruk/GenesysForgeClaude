using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Переносимый формат персонажа (бэкап / обмен между аккаунтами). Не содержит OwnerUserId и
/// internal id — ссылки на справочный контент идут по стабильному <c>Code</c> + <c>Name</c>.
/// </summary>
public record CharacterExportDto(
    string Format,
    DateTime ExportedAt,
    CharacterExportData Character)
{
    /// <summary>
    /// Текущая версия формата: v7 переносит выбор Improved и бесплатное улучшение Supreme
    /// именного оружия (ROT-HA-05).
    /// </summary>
    public const string CurrentFormat = "genesysforge.character.v7";

    /// <summary>v6 знал базовое улучшение именного оружия, но не его Improved и Supreme.</summary>
    public const string LegacyFormatV6 = "genesysforge.character.v6";

    /// <summary>
    /// v5 переносил груз транспорта и тягу (ROT-TRANSPORT-01), но именное оружие в нём ещё без
    /// базового улучшения: такой персонаж импортируется с незавершённым параметром.
    /// </summary>
    public const string LegacyFormatV5 = "genesysforge.character.v5";

    /// <summary>v4 переносил скакунов, но их груз был одним числом без позиций.</summary>
    public const string LegacyFormatV4 = "genesysforge.character.v4";

    /// <summary>v3 переносил настройку экземпляра Lesser Rune, но ещё не знал скакунов.</summary>
    public const string LegacyFormatV3 = "genesysforge.character.v3";

    public const string LegacyFormatV2 = "genesysforge.character.v2";

    /// <summary>Предыдущая версия формата — принимается на импорт с предупреждениями.</summary>
    public const string LegacyFormatV1 = "genesysforge.character.v1";

    /// <summary>Все форматы, которые импорт умеет читать.</summary>
    public static readonly string[] SupportedFormats =
        [CurrentFormat, LegacyFormatV6, LegacyFormatV5, LegacyFormatV4, LegacyFormatV3, LegacyFormatV2, LegacyFormatV1];
}

public record CharacterExportData(
    string Name,
    GameSystem System,
    string ArchetypeCode,
    string ArchetypeName,
    string CareerCode,
    string CareerName,
    Dictionary<string, int> Characteristics,
    int TotalXp,
    int SpentXp,
    int Money,
    bool IsCreationPhase,
    int WoundsCurrent,
    int StrainCurrent,
    List<CharacterSkillExport> Skills,
    List<CharacterTalentExport> Talents,
    List<CharacterItemExport> Items,
    string? HeroicAbilityCode,
    string? HeroicAbilityName,
    int HeroicUpgradeRank,
    List<CharacterNoteExport> Notes,
    int HeroicDurationRanks = 0,
    int HeroicFrequencyRanks = 0,
    bool HeroicStoryUpgrade = false,
    List<string>? HeroicSecondaryEffectCodes = null,
    // v2: зафиксированные при завершении создания пороги. null у персонажа в фазе создания
    // и у файлов v1 — импорт в этом случае предупреждает и не выдумывает точное значение.
    int? CreationWoundThreshold = null,
    int? CreationStrainThreshold = null,
    ThresholdSnapshotProvenance ThresholdSnapshotProvenance = ThresholdSnapshotProvenance.None,
    bool RulesReviewRequired = false,
    // v2: режим стартового снаряжения и остаток бюджета создания (ROT-CRE-03).
    StartingEquipmentMode StartingEquipmentMode = StartingEquipmentMode.StandardMoney,
    int StartingPurchaseBudget = 0,
    // v2: обязательный видовой выбор (Half-Catfolk). Пусто — вид выбора не требует либо
    // legacy-персонаж, которому выбор нужно сделать вручную (ROT-SPECIES-01).
    string SpeciesAbilityChoiceCode = "",
    // v2: личное название и происхождение героической способности (ROT-HA-01). Пусто у файлов v1
    // и у legacy-персонажей — импорт предупреждает и оставляет личность незаполненной.
    string? HeroicCustomName = null,
    HeroicOriginMode? HeroicOriginMode = null,
    HeroicOriginType? HeroicOriginPrimary = null,
    HeroicOriginType? HeroicOriginSecondary = null,
    string? HeroicOriginNarrative = null,
    List<int>? HeroicOriginRolls = null,
    // v2: параметр primary effect (ROT-HA-02). Навык Paragon переносится по коду/имени, а не по id,
    // потому что id кастомного навыка в чужом аккаунте не существует.
    string? ParagonSkillCode = null,
    string? ParagonSkillName = null,
    string? SixthSenseSubject = null,
    SignatureWeaponProfile? SignatureWeaponProfile = null,
    WeaponCraftsmanship? SignatureWeaponCraftsmanship = null,
    string? SignatureWeaponForm = null,
    WeaponFormTraits? SignatureWeaponTraits = null,
    bool SignatureWeaponLost = false,
    // v6: базовое улучшение именного оружия (ROT-HA-02) — по стабильному коду, а не по id:
    // в другом аккаунте того же id нет.
    string? SignatureWeaponBaseAttachmentCode = null,
    // v7: выбор Improved и бесплатное улучшение Supreme (ROT-HA-05); улучшение — по коду.
    SignatureWeaponImprovement? SignatureWeaponImprovement = null,
    string? SignatureWeaponSupremeAttachmentCode = null,
    // v4: скакуны персонажа (ROT-MOUNT-ITEM-01). У файлов прежних версий их нет — там скакун мог
    // быть только позицией инвентаря, и её переносит список Items.
    List<CharacterMountExport>? Mounts = null);

public record CharacterSkillExport(string Code, string Name, int Ranks, bool IsCareer, int FreeRanks);

public record CharacterTalentExport(string Code, string Name, int Ranks, string GrantedCharacteristics,
    // v2: общий формат выборов ранга (ROT-TAL-03); пусто у файлов v1.
    List<CharacterTalentChoiceExport>? Choices = null, bool NeedsChoice = false);

public record CharacterTalentChoiceExport(int RankIndex, TalentChoiceKind Kind, string Value, string DisplayName);

/// <param name="Craftsmanship">
/// Качество изготовления экземпляра (ROT-WPN-02). Файлы прежних версий его не содержат — там
/// экземпляр обычной работы, и это не догадка, а правило для legacy.
/// </param>
/// <param name="DamageState">
/// Состояние повреждения экземпляра (GEN-EQP-DMG-01). Файлы прежних версий его не содержат —
/// такой экземпляр цел, и это правило для legacy, а не догадка.
/// </param>
public record CharacterItemExport(string Code, string Name, int Quantity, ItemState State,
    ItemProvenance Provenance = ItemProvenance.Purchased,
    WeaponCraftsmanship Craftsmanship = WeaponCraftsmanship.Steel,
    ItemDamageState DamageState = ItemDamageState.Undamaged,
    /// <summary>
    /// Материал магического инструмента и выбранные ведущим эффекты (ROT-MAG-IMP-01/MAT-01).
    /// Файлы прежних версий их не содержат — там дуб и ненастроенный экземпляр.
    /// </summary>
    ImplementMaterial Material = ImplementMaterial.Oak,
    string ImplementChoices = "",
    bool ImplementConfigured = false,
    string ShardActivationChoice = "",
    string ShardEffectAction = "",
    string ShardEffectChoice = "",
    bool ShardConfigured = false,
    /// <summary>
    /// Индекс транспорта из списка <c>Mounts</c>, на котором лежит позиция (ROT-TRANSPORT-01).
    /// <c>null</c> — позиция при персонаже. По индексу, а не по id: id в чужом аккаунте свой.
    /// </summary>
    int? CarriedByMountIndex = null,
    /// <summary>Снаряжение установлено на транспорт, а не сложено грузом.</summary>
    bool IsInstalledOnMount = false);

public record CharacterNoteExport(string Title, string Body);

/// <summary>
/// Транспорт в переносимом формате (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Ссылка на профиль идёт по
/// стабильному <paramref name="Code"/> с fallback на <paramref name="Name"/>: id профиля в чужом
/// аккаунте свой.
/// </summary>
/// <param name="CustomName">Кличка или название; пусто — показывается название профиля.</param>
/// <param name="DrawnByMountIndex">
/// Индекс тяглового животного в этом же списке; <c>null</c> — тяги нет. Груз в v5 переносится
/// позициями (см. <c>CharacterItemExport.CarriedByMountIndex</c>), а не числом.
/// </param>
public record CharacterMountExport(
    string Code,
    string Name,
    string CustomName = "",
    int WoundsCurrent = 0,
    int CarriedLoad = 0,
    bool IsActive = false,
    string Notes = "",
    ItemProvenance Provenance = ItemProvenance.Purchased,
    int? DrawnByMountIndex = null);
