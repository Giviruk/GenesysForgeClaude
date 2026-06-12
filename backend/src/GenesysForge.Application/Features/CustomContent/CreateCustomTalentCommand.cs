using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record CreateCustomTalentCommand(Guid UserId, CreateCustomTalentRequest Request) : ICommand<TalentDefDto>;
