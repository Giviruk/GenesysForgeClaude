namespace GenesysForge.Application.Dtos;

public record CampaignListItemDto(Guid Id, string Name, bool IsGm, int CharacterCount, DateTime CreatedAt);
