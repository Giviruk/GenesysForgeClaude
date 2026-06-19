namespace GenesysForge.Application.Abstractions;

/// <summary>Политика аутентификации, конфигурируемая инфраструктурой.</summary>
public interface IAuthPolicy
{
    /// <summary>
    /// Требовать подтверждённый e-mail для входа. По умолчанию false — приватный MVP
    /// не блокирует вход; включается для публичного запуска (Auth:RequireEmailConfirmation).
    /// </summary>
    bool RequireEmailConfirmation { get; }
}
