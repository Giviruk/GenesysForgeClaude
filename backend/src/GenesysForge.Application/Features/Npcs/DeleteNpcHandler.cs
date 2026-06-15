using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Npcs;

public class DeleteNpcHandler(IAppDbContext db) : ICommandHandler<DeleteNpcCommand, Unit>
{
    public async Task<Unit> Handle(DeleteNpcCommand command, CancellationToken ct = default)
    {
        var npc = await NpcMapper.GetOwnedAsync(db, command.UserId, command.Id, ct, tracking: true);
        db.Npcs.Remove(npc);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
