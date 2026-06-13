using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class UpdateCampaignNoteHandler(IAppDbContext db) : ICommandHandler<UpdateCampaignNoteCommand, CampaignNoteDto>
{
    public async Task<CampaignNoteDto> Handle(UpdateCampaignNoteCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        if (string.IsNullOrWhiteSpace(command.Request.Title))
            throw new DomainRuleException("Заголовок заметки не может быть пустым.");

        var note = await db.CampaignNotes.FirstOrDefaultAsync(
                n => n.Id == command.NoteId && n.CampaignId == command.CampaignId, ct)
            ?? throw new DomainRuleException("Заметка не найдена.");

        note.Title = command.Request.Title.Trim();
        note.Body = command.Request.Body ?? "";
        note.IsPrivate = command.Request.IsPrivate;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new CampaignNoteDto(note.Id, note.Title, note.Body, note.IsPrivate, note.CreatedAt, note.UpdatedAt);
    }
}
