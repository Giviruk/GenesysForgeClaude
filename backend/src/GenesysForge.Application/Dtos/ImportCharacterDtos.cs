using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>Результат предпросмотра импорта — что будет создано и какие ссылки не разрешились.</summary>
public record ImportPreviewDto(
    string Name,
    GameSystem System,
    string ArchetypeName,
    string CareerName,
    int TotalXp,
    int SpentXp,
    int SkillCount,
    int TalentCount,
    int ItemCount,
    int NoteCount,
    List<string> Warnings);

/// <summary>Результат импорта — id созданного персонажа и предупреждения о пропущенных ссылках.</summary>
public record ImportCharacterResult(Guid CharacterId, string Name, List<string> Warnings);
