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
    /// <summary>Текущая версия формата: v2 добавила зафиксированные пороги ран/стрейна (ROT-CRE-02).</summary>
    public const string CurrentFormat = "genesysforge.character.v2";

    /// <summary>Предыдущая версия формата — принимается на импорт с предупреждениями.</summary>
    public const string LegacyFormatV1 = "genesysforge.character.v1";

    /// <summary>Все форматы, которые импорт умеет читать.</summary>
    public static readonly string[] SupportedFormats = [CurrentFormat, LegacyFormatV1];
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
    List<int>? HeroicOriginRolls = null);

public record CharacterSkillExport(string Code, string Name, int Ranks, bool IsCareer, int FreeRanks);

public record CharacterTalentExport(string Code, string Name, int Ranks, string GrantedCharacteristics,
    // v2: общий формат выборов ранга (ROT-TAL-03); пусто у файлов v1.
    List<CharacterTalentChoiceExport>? Choices = null, bool NeedsChoice = false);

public record CharacterTalentChoiceExport(int RankIndex, TalentChoiceKind Kind, string Value, string DisplayName);

public record CharacterItemExport(string Code, string Name, int Quantity, ItemState State,
    ItemProvenance Provenance = ItemProvenance.Purchased);

public record CharacterNoteExport(string Title, string Body);
