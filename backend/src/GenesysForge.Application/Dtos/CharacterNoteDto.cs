namespace GenesysForge.Application.Dtos;

public record CharacterNoteDto(Guid Id, string Title, string Body, DateTime CreatedAt, DateTime UpdatedAt);
