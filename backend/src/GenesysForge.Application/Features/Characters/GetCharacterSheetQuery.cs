using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record GetCharacterSheetQuery(Guid UserId, Guid CharacterId) : IQuery<CharacterSheetDto>;
