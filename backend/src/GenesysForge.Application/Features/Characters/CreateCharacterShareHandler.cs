using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

public class CreateCharacterShareHandler(IAppDbContext db) : ICommandHandler<CreateCharacterShareCommand, CharacterShareResponse>
{
    public async Task<CharacterShareResponse> Handle(CreateCharacterShareCommand command, CancellationToken ct = default)
    {
        var character = await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: false, ct);
        var raw = CharacterShareTokens.NewRawToken();
        db.CharacterShareTokens.Add(new CharacterShareToken
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            TokenHash = CharacterShareTokens.Hash(raw),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return new CharacterShareResponse(raw, $"/share/{raw}");
    }
}
