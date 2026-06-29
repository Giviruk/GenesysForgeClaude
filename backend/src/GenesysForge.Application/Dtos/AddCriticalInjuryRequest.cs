namespace GenesysForge.Application.Dtos;

/// <summary>Запрос на добавление крит-ранения (U-23): из таблицы U-11 (RuleCode) или вручную (NameRu).</summary>
public record AddCriticalInjuryRequest(
    string? RuleCode, string? NameRu, string? Severity, int? RollResult, string? Notes);
