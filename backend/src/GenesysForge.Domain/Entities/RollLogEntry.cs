namespace GenesysForge.Domain.Entities;

/// <summary>
/// Запись о броске кубов в кампании (лог стола). Результат считается на клиенте (v1);
/// здесь бросок хранится для истории и realtime-показа другим участникам стола.
/// </summary>
public class RollLogEntry
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    /// <summary>Активная сцена на момент броска (если была).</summary>
    public Guid? SessionId { get; set; }

    /// <summary>Пользователь, совершивший бросок.</summary>
    public Guid ActorUserId { get; set; }
    /// <summary>Имя действующего лица на момент броска (персонаж/NPC/игрок).</summary>
    public required string ActorName { get; set; }
    /// <summary>За что бросок (навык/описание), необязательно.</summary>
    public string Label { get; set; } = "";

    /// <summary>Состав пула (JSON: RollPool).</summary>
    public required string PoolJson { get; set; }
    /// <summary>Нетто-результат символами (JSON: RollSymbols).</summary>
    public required string ResultJson { get; set; }
    /// <summary>Краткий человекочитаемый итог («2 успеха, 1 преимущество»).</summary>
    public string Summary { get; set; } = "";

    /// <summary>Секретный бросок мастера — виден только GM.</summary>
    public bool IsSecret { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
