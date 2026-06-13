using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Notes;

public record GetCharacterNotesQuery(Guid UserId, Guid CharacterId) : IQuery<List<CharacterNoteDto>>;
