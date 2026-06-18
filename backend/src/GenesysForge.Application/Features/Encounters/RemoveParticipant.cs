using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Encounters;

public record RemoveEncounterParticipantCommand(Guid UserId, Guid EncounterId, Guid ParticipantId) : ICommand<Unit>;

public class RemoveEncounterParticipantHandler(IAppDbContext db)
    : ICommandHandler<RemoveEncounterParticipantCommand, Unit>
{
    public async Task<Unit> Handle(RemoveEncounterParticipantCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.EncounterId, ct, tracking: true);

        var p = encounter.Participants.FirstOrDefault(x => x.Id == command.ParticipantId)
            ?? throw new DomainRuleException("Участник не найден.");
        db.EncounterParticipants.Remove(p);
        encounter.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
