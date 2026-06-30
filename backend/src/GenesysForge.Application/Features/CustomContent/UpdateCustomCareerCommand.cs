using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.CustomContent;

public record UpdateCustomCareerCommand(Guid UserId, Guid CareerId, CreateCustomCareerRequest Request) : ICommand<CareerDto>;
