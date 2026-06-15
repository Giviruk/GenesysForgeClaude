using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

public record CreateNpcCommand(Guid UserId, NpcInput Input) : ICommand<NpcDetailDto>;
