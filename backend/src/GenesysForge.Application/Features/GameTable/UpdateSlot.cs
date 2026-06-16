using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.GameTable;

public record UpdateSlotCommand(
    Guid UserId, Guid CampaignId, Guid SlotId, UpdateSlotRequest Request) : ICommand<GameSessionDto>;

public class UpdateSlotHandler(IAppDbContext db) : ICommandHandler<UpdateSlotCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(UpdateSlotCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var slot = session.Slots.FirstOrDefault(s => s.Id == command.SlotId)
            ?? throw new DomainRuleException("Слот не найден.");
        var r = command.Request;

        if (r.SlotType is { } type) slot.SlotType = type;
        if (r.Notes is not null) slot.Notes = r.Notes.Trim();
        // AssignedParticipantId: проверяем принадлежность сцене; явный сброс отправляется как Guid.Empty.
        if (r.AssignedParticipantId is { } pid)
        {
            if (pid == Guid.Empty) slot.AssignedParticipantId = null;
            else if (session.Participants.Any(p => p.Id == pid)) slot.AssignedParticipantId = pid;
            else throw new DomainRuleException("Назначаемый участник не найден в сцене.");
        }
        if (r.Order is { } order)
        {
            // Перемещение слота: переупорядочиваем по новой позиции.
            var ordered = session.Slots.OrderBy(s => s.Order).ToList();
            ordered.Remove(slot);
            var target = Math.Clamp(order, 0, ordered.Count);
            ordered.Insert(target, slot);
            for (var i = 0; i < ordered.Count; i++) ordered[i].Order = i;
        }
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return GameTableMapper.ToDto(session, isGm: true);
    }
}
