using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record UpdateCustomItemCommand(Guid UserId, Guid ItemDefId, CreateCustomItemRequest Request) : ICommand<ItemDefDto>;
