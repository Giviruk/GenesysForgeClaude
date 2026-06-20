using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Auth;

public record GoogleSignInCommand(GoogleSignInRequest Request) : ICommand<AuthResponse>;
