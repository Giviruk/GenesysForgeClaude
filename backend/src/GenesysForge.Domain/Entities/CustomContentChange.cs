namespace GenesysForge.Domain.Entities;

public enum CustomContentChangeAction { Created, Updated, Deleted }

/// <summary>История авторского контента; не зависит от жизненного цикла определения.</summary>
public class CustomContentChange
{
    public Guid Id { get; set; }
    public Guid? HomebrewPackId { get; set; }
    public required string DefinitionType { get; set; }
    public Guid DefinitionId { get; set; }
    public required string DefinitionName { get; set; }
    public Guid UserId { get; set; }
    public CustomContentChangeAction Action { get; set; }
    public string ChangesJson { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
