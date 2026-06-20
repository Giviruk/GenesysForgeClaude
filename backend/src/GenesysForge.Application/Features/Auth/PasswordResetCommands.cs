using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Auth;

public record RequestPasswordResetCommand(PasswordResetRequestRequest Request) : ICommand<Unit>;

public record ConfirmPasswordResetCommand(PasswordResetConfirmRequest Request) : ICommand<Unit>;
