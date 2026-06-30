namespace GenesysForge.Domain.Entities;

/// <summary>Opaque-token for public read-only access to a character sheet (U-24).</summary>
public class CharacterShareToken
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Character? Character { get; set; }
    /// <summary>SHA-256 hash of the raw URL token. The raw token is returned only once.</summary>
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}
