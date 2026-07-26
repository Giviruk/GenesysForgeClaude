namespace GenesysForge.Application.Dtos;

/// <param name="Message">Человекочитаемое сообщение для UI.</param>
/// <param name="ReasonCode">
/// Стабильный машинный код причины отказа, если правило его назначило. Клиент ветвится по нему,
/// а не по тексту <paramref name="Message"/>, который может быть переведён или переформулирован.
/// </param>
public record ErrorResponse(string Message, string? ReasonCode = null);
