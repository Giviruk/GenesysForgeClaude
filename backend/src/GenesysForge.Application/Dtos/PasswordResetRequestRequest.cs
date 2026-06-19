namespace GenesysForge.Application.Dtos;

/// <summary>Запрос на сброс пароля по e-mail (ответ всегда 204, без раскрытия наличия аккаунта).</summary>
public record PasswordResetRequestRequest(string Email);
