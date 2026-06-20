namespace GenesysForge.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    /// <summary>Подтверждён ли e-mail. Существующие до фичи аккаунты считаются подтверждёнными (миграция).</summary>
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
