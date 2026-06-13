namespace GenesysForge.Domain.Entities;

/// <summary>Произвольная заметка на листе персонажа (журнал, сюжет, напоминания).</summary>
public class CharacterNote
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>Автор заметки (владелец персонажа; в будущем — GM для campaign-заметок).</summary>
    public Guid OwnerUserId { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
