using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomSkillCommand(Guid UserId, Guid CampaignId, CreateCustomSkillRequest Request) : ICommand<SkillDefDto>;
