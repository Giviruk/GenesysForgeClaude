using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record UpdateCustomTalentCommand(Guid UserId, Guid TalentDefId, CreateCustomTalentRequest Request) : ICommand<TalentDefDto>;
