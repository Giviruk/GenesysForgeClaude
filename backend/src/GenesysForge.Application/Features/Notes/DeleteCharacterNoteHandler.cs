using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Notes;

public class DeleteCharacterNoteHandler(IAppDbContext db) : ICommandHandler<DeleteCharacterNoteCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCharacterNoteCommand command, CancellationToken ct = default)
    {
        var note = await db.CharacterNotes.FirstOrDefaultAsync(
                n => n.Id == command.NoteId && n.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Заметка не найдена.");

        db.CharacterNotes.Remove(note);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
