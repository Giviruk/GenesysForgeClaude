using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.CustomContent;

public record DeleteCustomSkillCommand(Guid UserId, Guid SkillDefId) : ICommand<Unit>;
