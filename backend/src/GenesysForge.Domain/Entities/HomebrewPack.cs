namespace GenesysForge.Domain.Entities;

/// <summary>Переносимый пользовательский набор homebrew-контента.</summary>
public class HomebrewPack
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public GameSystem System { get; set; }
    /// <summary>Публичный токен позволяет другим пользователям импортировать копию набора.</summary>
    public string? ShareTokenHash { get; set; }
    public bool IsShared { get; set; }
    /// <summary>Если true, владелец видит контент набора без character/campaign context.</summary>
    public bool IsEnabledByDefault { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
