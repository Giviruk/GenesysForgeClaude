namespace GenesysForge.Application.Dtos;

public record CampaignNoteDto(Guid Id, string Title, string Body, bool IsPrivate, DateTime CreatedAt, DateTime UpdatedAt);
