using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.CustomContent;

public record DeleteCustomTalentCommand(Guid UserId, Guid TalentDefId) : ICommand<Unit>;
