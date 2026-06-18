using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record EncounterParticipantDto(
    Guid Id,
    Guid? CharacterId,
    Guid? NpcId,
    string DisplayName,
    ParticipantType ParticipantType,
    InitiativeSlotType InitiativeSide,
    int Quantity,
    string Notes,
    bool StartsHidden,
    bool StartsDefeated,
    int? StartingWoundsOverride,
    int? StartingStrainOverride,
    int Order);

public record EncounterListItemDto(
    Guid Id,
    string Name,
    GameSystem System,
    EncounterType Type,
    ThreatLevel ThreatLevel,
    bool IsVisibleToPlayers,
    int ParticipantCount,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Детальная сцена. Приватные поля заполняются null для игрока (см. спецификацию §7).</summary>
public record EncounterDetailDto(
    Guid Id,
    Guid CampaignId,
    string Name,
    GameSystem System,
    EncounterType Type,
    ThreatLevel ThreatLevel,
    bool IsGm,
    bool IsVisibleToPlayers,
    string? GmDescription,
    string PlayerDescription,
    string PlayerGoals,
    string? NpcGoals,
    string Location,
    string Environment,
    string? Complications,
    string Rewards,
    IReadOnlyList<string> Tags,
    IReadOnlyList<EncounterParticipantDto> Participants,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Создание/обновление сцены. Участники редактируются отдельными вызовами.</summary>
public record EncounterInput(
    string Name,
    GameSystem System,
    EncounterType Type,
    ThreatLevel ThreatLevel,
    string? GmDescription,
    string? PlayerDescription,
    string? PlayerGoals,
    string? NpcGoals,
    string? Location,
    string? Environment,
    string? Complications,
    string? Rewards,
    bool IsVisibleToPlayers,
    List<string>? Tags);

/// <summary>Добавление участника: из персонажа (CharacterId), из NPC (NpcId) или вручную (DisplayName).</summary>
public record AddEncounterParticipantRequest(
    Guid? CharacterId,
    Guid? NpcId,
    string? DisplayName,
    ParticipantType? ParticipantType,
    InitiativeSlotType? InitiativeSide,
    int? Quantity,
    string? Notes,
    bool? StartsHidden,
    bool? StartsDefeated,
    int? StartingWoundsOverride,
    int? StartingStrainOverride);

public record UpdateEncounterParticipantRequest(
    string? DisplayName,
    InitiativeSlotType? InitiativeSide,
    int? Quantity,
    string? Notes,
    bool? StartsHidden,
    bool? StartsDefeated,
    int? StartingWoundsOverride,
    int? StartingStrainOverride);

/// <summary>Массовое добавление персонажей кампании. Пусто/null → добавить всех активных PC.</summary>
public record AddCampaignCharactersRequest(List<Guid>? CharacterIds);

/// <summary>
/// Поведение при наличии активной сцены Game Table:
/// Replace — завершить текущую и создать новую; Append — добавить участников в текущую.
/// </summary>
public enum SendToTableMode
{
    Replace = 0,
    Append = 1,
}

public record SendToTableRequest(SendToTableMode Mode);
