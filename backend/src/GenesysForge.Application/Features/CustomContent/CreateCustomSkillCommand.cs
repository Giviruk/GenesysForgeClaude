using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomSkillCommand(Guid UserId, CreateCustomSkillRequest Request) : ICommand<SkillDefDto>;
