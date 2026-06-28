namespace GenesysForge.Application.Dtos;

/// <summary>Профиль текущего пользователя (U-21).</summary>
public record AccountDto(Guid Id, string Email, string DisplayName, string? AvatarUrl, DateTime CreatedAt);

/// <summary>Изменение профиля: имя и (опц.) URL аватара. Null-поля не меняются.</summary>
public record UpdateAccountRequest(string? DisplayName, string? AvatarUrl);

/// <summary>Смена пароля в текущей сессии (требует текущий пароль).</summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
