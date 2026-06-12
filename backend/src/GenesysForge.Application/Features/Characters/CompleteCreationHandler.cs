using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;

namespace GenesysForge.Application.Features.Characters;

public class CompleteCreationHandler(IAppDbContext db) : ICommandHandler<CompleteCreationCommand, Unit>
{
    public async Task<Unit> Handle(CompleteCreationCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        c.IsCreationPhase = false;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
