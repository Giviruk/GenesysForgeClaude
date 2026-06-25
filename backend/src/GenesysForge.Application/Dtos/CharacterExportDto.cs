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
    /// <summary>Текущая версия формата.</summary>
    public const string CurrentFormat = "genesysforge.character.v1";
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
    List<CharacterNoteExport> Notes);

public record CharacterSkillExport(string Code, string Name, int Ranks, bool IsCareer, int FreeRanks);

public record CharacterTalentExport(string Code, string Name, int Ranks, string GrantedCharacteristics);

public record CharacterItemExport(string Code, string Name, int Quantity, ItemState State);

public record CharacterNoteExport(string Title, string Body);
