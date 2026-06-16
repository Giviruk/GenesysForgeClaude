using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Следующий ход: продвигает индекс слота, после последнего — новый раунд.</summary>
public record NextTurnCommand(Guid UserId, Guid CampaignId) : ICommand<GameSessionDto>;

public class NextTurnHandler(IAppDbContext db) : ICommandHandler<NextTurnCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(NextTurnCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var slotCount = session.Slots.Count;
        if (slotCount == 0)
        {
            session.CurrentTurnIndex = 0;
            session.CurrentRound++;
        }
        else if (session.CurrentTurnIndex + 1 >= slotCount)
        {
            session.CurrentTurnIndex = 0;
            session.CurrentRound++;
        }
        else
        {
            session.CurrentTurnIndex++;
        }
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return GameTableMapper.ToDto(session, isGm: true);
    }
}
