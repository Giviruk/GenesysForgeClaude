using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

public record DuplicateNpcCommand(Guid UserId, Guid Id) : ICommand<NpcDetailDto>;
