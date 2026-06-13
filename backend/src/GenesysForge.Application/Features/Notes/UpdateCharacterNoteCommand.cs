using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Notes;

public record UpdateCharacterNoteCommand(Guid UserId, Guid NoteId, SaveCharacterNoteRequest Request)
    : ICommand<CharacterNoteDto>;
