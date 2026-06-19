namespace GenesysForge.Application.Dtos;

/// <summary>Подтверждение e-mail по одноразовому токену из письма.</summary>
public record ConfirmEmailRequest(string Token);

/// <summary>Повторная отправка письма подтверждения (ответ всегда 204).</summary>
public record ResendEmailConfirmationRequest(string Email);
