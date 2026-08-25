using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record CharacterAuditEntryDto(
    Guid Id,
    DateTime CreatedAt,
    CharacterAuditAction Action,
    string Summary,
    int? XpDelta,
    int TotalXpAfter,
    int SpentXpAfter,
    bool CanUndo = false);

/// <summary>Выдача (или коррекция) суммарного XP мастером/владельцем. Amount может быть отрицательным.</summary>
public record AwardXpRequest(int Amount, string? Note);
