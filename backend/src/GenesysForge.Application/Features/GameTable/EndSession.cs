using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Завершение сцены: удаляет активную сессию кампании целиком.</summary>
public record EndSessionCommand(Guid UserId, Guid CampaignId) : ICommand<Unit>;

public class EndSessionHandler(IAppDbContext db) : ICommandHandler<EndSessionCommand, Unit>
{
    public async Task<Unit> Handle(EndSessionCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);
        db.GameSessions.Remove(session);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
