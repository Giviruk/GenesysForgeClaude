namespace GenesysForge.Application.Dtos;

/// <summary>Установка нового пароля по одноразовому токену из письма.</summary>
public record PasswordResetConfirmRequest(string Token, string NewPassword);
