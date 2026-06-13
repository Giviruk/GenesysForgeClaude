using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Notes;

public record CreateCharacterNoteCommand(Guid UserId, Guid CharacterId, SaveCharacterNoteRequest Request)
    : ICommand<CharacterNoteDto>;
