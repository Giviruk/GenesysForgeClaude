using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.CustomContent;

public record DeleteCustomArchetypeCommand(Guid UserId, Guid ArchetypeId) : ICommand<Unit>;
