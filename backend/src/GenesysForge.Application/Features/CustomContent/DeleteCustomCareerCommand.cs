using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.CustomContent;

public record DeleteCustomCareerCommand(Guid UserId, Guid CareerId) : ICommand<Unit>;
