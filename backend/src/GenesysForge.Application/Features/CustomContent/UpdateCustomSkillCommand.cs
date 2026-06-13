using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record UpdateCustomSkillCommand(Guid UserId, Guid SkillDefId, CreateCustomSkillRequest Request) : ICommand<SkillDefDto>;
