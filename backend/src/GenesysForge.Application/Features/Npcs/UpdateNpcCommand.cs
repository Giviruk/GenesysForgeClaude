using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

public record UpdateNpcCommand(Guid UserId, Guid Id, NpcInput Input) : ICommand<NpcDetailDto>;
