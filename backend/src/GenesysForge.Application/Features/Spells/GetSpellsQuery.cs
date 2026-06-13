using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Spells;

public record GetSpellsQuery(Guid UserId, GameSystem System) : IQuery<List<SpellDto>>;
