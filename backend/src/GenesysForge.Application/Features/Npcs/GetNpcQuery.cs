using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

public record GetNpcQuery(Guid UserId, Guid Id) : IQuery<NpcDetailDto>;
