using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Encounters;

public record DeleteEncounterCommand(Guid UserId, Guid Id) : ICommand<Unit>;

public class DeleteEncounterHandler(IAppDbContext db) : ICommandHandler<DeleteEncounterCommand, Unit>
{
    public async Task<Unit> Handle(DeleteEncounterCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.Id, ct, tracking: true);
        db.Encounters.Remove(encounter);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
