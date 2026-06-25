using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record ImportCharacterCommand(Guid UserId, CharacterExportDto Payload) : ICommand<ImportCharacterResult>;
