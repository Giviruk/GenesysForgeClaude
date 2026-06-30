using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record UpdateCustomArchetypeCommand(Guid UserId, Guid ArchetypeId, CreateCustomArchetypeRequest Request)
    : ICommand<ArchetypeDto>;
