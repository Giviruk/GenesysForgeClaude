using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;

namespace GenesysForge.Application.Features.Characters;

public class DeleteCharacterHandler(IAppDbContext db) : ICommandHandler<DeleteCharacterCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCharacterCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        db.Characters.Remove(c);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
