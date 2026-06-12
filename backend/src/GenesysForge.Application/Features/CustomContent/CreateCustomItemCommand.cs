using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomItemCommand(Guid UserId, CreateCustomItemRequest Request) : ICommand<ItemDefDto>;
