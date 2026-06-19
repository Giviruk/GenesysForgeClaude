using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Auth;

public record ConfirmEmailCommand(ConfirmEmailRequest Request) : ICommand<Unit>;

public record ResendEmailConfirmationCommand(ResendEmailConfirmationRequest Request) : ICommand<Unit>;
