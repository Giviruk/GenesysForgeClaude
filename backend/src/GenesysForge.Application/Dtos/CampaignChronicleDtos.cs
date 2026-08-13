namespace GenesysForge.Application.Dtos;

public record CampaignChronicleChapterDto(
    Guid Id,
    string Title,
    string Content,
    int SortOrder,
    int CurrentVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string UpdatedBy);

public record CampaignChronicleRevisionDto(
    Guid Id,
    int Version,
    string Title,
    string Content,
    DateTime EditedAt,
    string EditedBy);

public record SaveCampaignChronicleChapterRequest(string Title, string Content, int? ExpectedVersion = null);
