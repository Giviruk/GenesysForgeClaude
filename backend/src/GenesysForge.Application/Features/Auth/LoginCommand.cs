using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Auth;

public record LoginCommand(LoginRequest Request) : ICommand<AuthResponse>;
