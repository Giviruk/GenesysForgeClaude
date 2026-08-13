using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomItemCommand(Guid UserId, Guid CampaignId, CreateCustomItemRequest Request) : ICommand<ItemDefDto>;
