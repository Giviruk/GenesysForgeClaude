using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record GetSharedCharacterSheetQuery(string Token) : IQuery<CharacterSheetDto>;
