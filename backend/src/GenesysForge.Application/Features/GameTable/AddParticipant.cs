using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Application.Features.GameTable;

public record AddParticipantCommand(Guid UserId, Guid CampaignId, AddParticipantRequest Request) : ICommand<GameSessionDto>;

public class AddParticipantHandler(IAppDbContext db) : ICommandHandler<AddParticipantCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(AddParticipantCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var participant = await ParticipantFactory.CreateAsync(db, session.Id, command.CampaignId, command.Request, ct);
        participant.Order = session.Participants.Count == 0 ? 0 : session.Participants.Max(p => p.Order) + 1;
        db.GameParticipants.Add(participant);
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var fresh = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct);
        return GameTableMapper.ToDto(fresh, isGm: true);
    }
}
