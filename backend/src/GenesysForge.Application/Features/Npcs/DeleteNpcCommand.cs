using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Npcs;

public record DeleteNpcCommand(Guid UserId, Guid Id) : ICommand<Unit>;
