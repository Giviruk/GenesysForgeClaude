namespace GenesysForge.Domain.Entities;

/// <summary>
/// Запись истории персонажа (audit log): за что выдан/потрачен XP, что куплено/возвращено.
/// Пишется в той же транзакции, что и сама операция, поэтому отражает состояние после неё.
/// </summary>
public class CharacterAuditEntry
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>Пользователь, выполнивший операцию (владелец персонажа).</summary>
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CharacterAuditAction Action { get; set; }
    /// <summary>Краткое человекочитаемое описание операции.</summary>
    public string Summary { get; set; } = "";
    /// <summary>Изменение доступного XP (покупка — отрицательное, рефанд/награда — положительное); null для операций без XP.</summary>
    public int? XpDelta { get; set; }
    public int TotalXpAfter { get; set; }
    public int SpentXpAfter { get; set; }

    /// <summary>Структурные детали операции (JSON) — для будущих фильтров/тултипов.</summary>
    public string DataJson { get; set; } = "";
}
