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
    bool RulesReviewRequired = false);

public record CharacterSkillExport(string Code, string Name, int Ranks, bool IsCareer, int FreeRanks);

public record CharacterTalentExport(string Code, string Name, int Ranks, string GrantedCharacteristics);

public record CharacterItemExport(string Code, string Name, int Quantity, ItemState State);

public record CharacterNoteExport(string Title, string Body);
