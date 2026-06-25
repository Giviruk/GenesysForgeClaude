namespace GenesysForge.Application.Dtos;

public record RollLogEntryDto(
    Guid Id,
    Guid CampaignId,
    Guid? SessionId,
    string ActorName,
    string Label,
    string PoolJson,
    string ResultJson,
    string Summary,
    bool IsSecret,
    DateTime CreatedAt);

/// <summary>
/// Бросок, посчитанный на клиенте, для записи в лог стола. Поля Pool/Result — JSON-снимки
/// (структуры на стороне клиента). Секретный бросок учитывается только у мастера.
/// </summary>
public record CreateRollRequest(
    string? ActorName,
    string? Label,
    string PoolJson,
    string ResultJson,
    string? Summary,
    bool IsSecret);
