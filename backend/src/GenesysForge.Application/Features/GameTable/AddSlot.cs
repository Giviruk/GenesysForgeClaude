using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.GameTable;

public record AddSlotCommand(Guid UserId, Guid CampaignId, AddSlotRequest Request) : ICommand<GameSessionDto>;

public class AddSlotHandler(IAppDbContext db) : ICommandHandler<AddSlotCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(AddSlotCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var slot = new InitiativeSlot
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            SlotType = command.Request.SlotType,
            Order = session.Slots.Count == 0 ? 0 : session.Slots.Max(s => s.Order) + 1,
            AssignedParticipantId = command.Request.AssignedParticipantId,
            Notes = command.Request.Notes?.Trim() ?? "",
        };
        db.InitiativeSlots.Add(slot);
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var fresh = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct);
        return GameTableMapper.ToDto(fresh, isGm: true);
    }
}
