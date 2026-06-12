using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Abstractions;

public interface IPasswordHasherService
{
    string Hash(User user, string password);
    bool Verify(User user, string passwordHash, string password);
}
