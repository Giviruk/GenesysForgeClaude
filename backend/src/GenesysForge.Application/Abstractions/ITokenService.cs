using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Abstractions;

public interface ITokenService
{
    string CreateToken(User user);
}
