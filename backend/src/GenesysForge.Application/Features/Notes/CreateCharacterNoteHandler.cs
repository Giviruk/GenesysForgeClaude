using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Notes;

public class CreateCharacterNoteHandler(IAppDbContext db) : ICommandHandler<CreateCharacterNoteCommand, CharacterNoteDto>
{
    public async Task<CharacterNoteDto> Handle(CreateCharacterNoteCommand command, CancellationToken ct = default)
    {
        await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: false, ct);
        if (string.IsNullOrWhiteSpace(command.Request.Title))
            throw new DomainRuleException("Заголовок заметки не может быть пустым.");

        var now = DateTime.UtcNow;
        var note = new CharacterNote
        {
            Id = Guid.NewGuid(),
            CharacterId = command.CharacterId,
            OwnerUserId = command.UserId,
            Title = command.Request.Title.Trim(),
            Body = command.Request.Body ?? "",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CharacterNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return new CharacterNoteDto(note.Id, note.Title, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
