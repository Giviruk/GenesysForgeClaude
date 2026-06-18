using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Encounters;

public record AddEncounterParticipantCommand(Guid UserId, Guid EncounterId, AddEncounterParticipantRequest Request)
    : ICommand<EncounterDetailDto>;

public class AddEncounterParticipantHandler(IAppDbContext db)
    : ICommandHandler<AddEncounterParticipantCommand, EncounterDetailDto>
{
    public async Task<EncounterDetailDto> Handle(AddEncounterParticipantCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.EncounterId, ct, tracking: true);

        var participant = await EncounterParticipantFactory.CreateAsync(
            db, encounter.Id, encounter.CampaignId, command.Request, ct);
        participant.Order = encounter.Participants.Count == 0 ? 0 : encounter.Participants.Max(p => p.Order) + 1;
        encounter.Participants.Add(participant);
        encounter.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return EncounterMapper.ToDetail(encounter, isGm: true);
    }
}
