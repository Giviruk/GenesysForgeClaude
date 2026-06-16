using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Сброс сцены: убирает участников и слоты, обнуляет раунд/ход (сцена остаётся активной).</summary>
public record ResetSessionCommand(Guid UserId, Guid CampaignId) : ICommand<GameSessionDto>;

public class ResetSessionHandler(IAppDbContext db) : ICommandHandler<ResetSessionCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(ResetSessionCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        db.GameParticipants.RemoveRange(session.Participants);
        db.InitiativeSlots.RemoveRange(session.Slots);
        session.Participants.Clear();
        session.Slots.Clear();
        session.CurrentRound = 1;
        session.CurrentTurnIndex = 0;
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return GameTableMapper.ToDto(session, isGm: true);
    }
}
