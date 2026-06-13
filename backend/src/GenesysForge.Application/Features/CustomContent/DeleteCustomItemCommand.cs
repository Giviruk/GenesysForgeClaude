using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.CustomContent;

public record DeleteCustomItemCommand(Guid UserId, Guid ItemDefId) : ICommand<Unit>;
