using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class DeleteCampaignNoteHandler(IAppDbContext db) : ICommandHandler<DeleteCampaignNoteCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCampaignNoteCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var note = await db.CampaignNotes.FirstOrDefaultAsync(
                n => n.Id == command.NoteId && n.CampaignId == command.CampaignId, ct)
            ?? throw new DomainRuleException("Заметка не найдена.");

        db.CampaignNotes.Remove(note);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
