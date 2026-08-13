using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomArchetypeCommand(Guid UserId, Guid CampaignId, CreateCustomArchetypeRequest Request) : ICommand<ArchetypeDto>;
