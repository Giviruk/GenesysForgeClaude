using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Notes;

public class UpdateCharacterNoteHandler(IAppDbContext db) : ICommandHandler<UpdateCharacterNoteCommand, CharacterNoteDto>
{
    public async Task<CharacterNoteDto> Handle(UpdateCharacterNoteCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Title))
            throw new DomainRuleException("Заголовок заметки не может быть пустым.");

        var note = await db.CharacterNotes.FirstOrDefaultAsync(
                n => n.Id == command.NoteId && n.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Заметка не найдена.");

        note.Title = command.Request.Title.Trim();
        note.Body = command.Request.Body ?? "";
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new CharacterNoteDto(note.Id, note.Title, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
