using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.GameTable;

public record RemoveParticipantCommand(Guid UserId, Guid CampaignId, Guid ParticipantId) : ICommand<Unit>;

public class RemoveParticipantHandler(IAppDbContext db) : ICommandHandler<RemoveParticipantCommand, Unit>
{
    public async Task<Unit> Handle(RemoveParticipantCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var participant = session.Participants.FirstOrDefault(p => p.Id == command.ParticipantId)
            ?? throw new DomainRuleException("Участник не найден.");
        // освобождаем слоты, ссылающиеся на участника
        foreach (var slot in session.Slots.Where(s => s.AssignedParticipantId == participant.Id))
            slot.AssignedParticipantId = null;
        db.GameParticipants.Remove(participant);
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
