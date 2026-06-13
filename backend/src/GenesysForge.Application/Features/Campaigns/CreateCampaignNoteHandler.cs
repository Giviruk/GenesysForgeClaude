using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Campaigns;

public class CreateCampaignNoteHandler(IAppDbContext db) : ICommandHandler<CreateCampaignNoteCommand, CampaignNoteDto>
{
    public async Task<CampaignNoteDto> Handle(CreateCampaignNoteCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        if (string.IsNullOrWhiteSpace(command.Request.Title))
            throw new DomainRuleException("Заголовок заметки не может быть пустым.");

        var now = DateTime.UtcNow;
        var note = new CampaignNote
        {
            Id = Guid.NewGuid(),
            CampaignId = command.CampaignId,
            Title = command.Request.Title.Trim(),
            Body = command.Request.Body ?? "",
            IsPrivate = command.Request.IsPrivate,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CampaignNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return new CampaignNoteDto(note.Id, note.Title, note.Body, note.IsPrivate, note.CreatedAt, note.UpdatedAt);
    }
}
