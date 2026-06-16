using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.GameTable;

public record RemoveSlotCommand(Guid UserId, Guid CampaignId, Guid SlotId) : ICommand<Unit>;

public class RemoveSlotHandler(IAppDbContext db) : ICommandHandler<RemoveSlotCommand, Unit>
{
    public async Task<Unit> Handle(RemoveSlotCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var slot = session.Slots.FirstOrDefault(s => s.Id == command.SlotId)
            ?? throw new DomainRuleException("Слот не найден.");
        db.InitiativeSlots.Remove(slot);
        session.Slots.Remove(slot);
        // нормализуем порядок и текущий индекс
        var ordered = session.Slots.OrderBy(s => s.Order).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].Order = i;
        if (session.CurrentTurnIndex >= ordered.Count)
            session.CurrentTurnIndex = ordered.Count == 0 ? 0 : ordered.Count - 1;
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
