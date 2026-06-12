using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Auth;

public record RegisterUserCommand(RegisterRequest Request) : ICommand<AuthResponse>;
