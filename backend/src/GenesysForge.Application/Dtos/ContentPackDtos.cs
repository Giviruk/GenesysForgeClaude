using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record ContentPackEntryDto(
    Guid Id,
    ContentEntryType ContentType,
    Guid? ContentId,
    string Title,
    AllowedState AllowedState,
    HouseRuleCategory Category,
    string SafeSummary,
    string Source,
    string PageRef,
    string? GmNotes,
    string PlayerNotes,
    IReadOnlyList<string> Tags,
    int SortOrder);

public record ContentPackListItemDto(
    Guid Id,
    string Name,
    GameSystem System,
    bool IsPublicToCampaign,
    int EntryCount,
    DateTime UpdatedAt);

public record ContentPackDetailDto(
    Guid Id,
    Guid CampaignId,
    string Name,
    string Description,
    GameSystem System,
    bool IsGm,
    bool IsPublicToCampaign,
    IReadOnlyList<ContentPackEntryDto> Entries,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateContentPackRequest(string Name, string? Description, GameSystem System);

public record UpdateContentPackRequest(
    string? Name, string? Description, GameSystem? System, bool? IsPublicToCampaign);

public record ContentPackEntryInput(
    ContentEntryType ContentType,
    Guid? ContentId,
    string Title,
    AllowedState AllowedState,
    HouseRuleCategory? Category,
    string? SafeSummary,
    string? Source,
    string? PageRef,
    string? GmNotes,
    string? PlayerNotes,
    List<string>? Tags);
