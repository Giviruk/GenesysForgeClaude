using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record InitiativeSlotDto(
    Guid Id, InitiativeSlotType SlotType, int Order, Guid? AssignedParticipantId, string Notes);

public record GameParticipantDto(
    Guid Id,
    Guid? CharacterId,
    Guid? NpcId,
    string DisplayName,
    ParticipantType ParticipantType,
    InitiativeSlotType InitiativeSlotType,
    int Count,
    int WoundsCurrent,
    int WoundsThreshold,
    int StrainCurrent,
    int? StrainThreshold,
    int Soak,
    int MeleeDefense,
    int RangedDefense,
    bool IsActive,
    bool IsDefeated,
    bool IsHiddenFromPlayers,
    string Notes,
    int Order);

public record GameSessionDto(
    Guid Id,
    Guid CampaignId,
    string Name,
    string Description,
    bool IsActive,
    bool IsGm,
    bool AllowPlayerEdits,
    int PlayerStoryPoints,
    int GmStoryPoints,
    int CurrentRound,
    int CurrentTurnIndex,
    string PublicNotes,
    string? GmNotes,
    IReadOnlyList<GameParticipantDto> Participants,
    IReadOnlyList<InitiativeSlotDto> Slots);

public record CreateSessionRequest(string Name, string? Description, int PlayerStoryPoints, int GmStoryPoints);

public record UpdateSessionRequest(
    string? Name,
    string? Description,
    string? PublicNotes,
    string? GmNotes,
    int? PlayerStoryPoints,
    int? GmStoryPoints,
    bool? AllowPlayerEdits);

/// <summary>Добавление участника: из персонажа (CharacterId), из NPC (NpcId) или вручную.</summary>
public record AddParticipantRequest(
    Guid? CharacterId,
    Guid? NpcId,
    string? DisplayName,
    ParticipantType? ParticipantType,
    InitiativeSlotType? InitiativeSlotType,
    int? Count,
    int? WoundsThreshold,
    int? StrainThreshold,
    int? Soak,
    int? MeleeDefense,
    int? RangedDefense);

public record UpdateParticipantRequest(
    string? DisplayName,
    int? WoundsCurrent,
    int? WoundsThreshold,
    int? StrainCurrent,
    int? StrainThreshold,
    int? Soak,
    int? MeleeDefense,
    int? RangedDefense,
    bool? IsActive,
    bool? IsDefeated,
    bool? IsHiddenFromPlayers,
    string? Notes,
    InitiativeSlotType? InitiativeSlotType);

public record AddSlotRequest(InitiativeSlotType SlotType, Guid? AssignedParticipantId, string? Notes);

public record UpdateSlotRequest(InitiativeSlotType? SlotType, int? Order, Guid? AssignedParticipantId, string? Notes);
