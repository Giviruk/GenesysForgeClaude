using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Notes;

public record DeleteCharacterNoteCommand(Guid UserId, Guid NoteId) : ICommand<Unit>;
