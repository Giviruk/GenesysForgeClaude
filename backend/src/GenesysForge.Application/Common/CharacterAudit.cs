using System.Text.Json;
using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

/// <summary>
/// Запись истории персонажа. Добавляет `CharacterAuditEntry` в контекст; фактическая
/// фиксация происходит вместе с операцией (общий `SaveChangesAsync`) — атомарно.
/// </summary>
public static class CharacterAudit
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Русские метки характеристик (как на фронте) для summary истории.</summary>
    public static string CharacteristicLabel(CharacteristicType type) => type switch
    {
        CharacteristicType.Brawn => "Мощь",
        CharacteristicType.Agility => "Ловкость",
        CharacteristicType.Intellect => "Интеллект",
        CharacteristicType.Cunning => "Хитрость",
        CharacteristicType.Willpower => "Воля",
        CharacteristicType.Presence => "Харизма",
        _ => type.ToString(),
    };

    public static void Record(
        IAppDbContext db, Character c, Guid userId, CharacterAuditAction action,
        string summary, int? xpDelta = null, object? data = null)
    {
        db.CharacterAuditEntries.Add(new CharacterAuditEntry
        {
            Id = Guid.NewGuid(),
            CharacterId = c.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Action = action,
            Summary = summary,
            XpDelta = xpDelta,
            TotalXpAfter = c.TotalXp,
            SpentXpAfter = c.SpentXp,
            DataJson = data is null ? "" : JsonSerializer.Serialize(data, JsonOptions),
        });
    }
}
