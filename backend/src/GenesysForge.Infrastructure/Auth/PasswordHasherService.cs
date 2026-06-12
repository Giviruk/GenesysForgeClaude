using GenesysForge.Application.Abstractions;
using GenesysForge.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>Адаптер ASP.NET Identity PasswordHasher под абстракцию Application-слоя.</summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(user, passwordHash, password) != PasswordVerificationResult.Failed;
}
