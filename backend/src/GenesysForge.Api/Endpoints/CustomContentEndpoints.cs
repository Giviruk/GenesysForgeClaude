using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.CustomContent;

namespace GenesysForge.Api.Endpoints;

public static class CustomContentEndpoints
{
    public static void MapCustomContent(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/custom").RequireAuthorization();

        group.MapPost("/skills", async (CreateCustomSkillRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCustomSkillCommand, SkillDefDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCustomSkillCommand(user.UserId(), req), ct)));

        group.MapPost("/talents", async (CreateCustomTalentRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCustomTalentCommand, TalentDefDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCustomTalentCommand(user.UserId(), req), ct)));

        group.MapPost("/items", async (CreateCustomItemRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCustomItemCommand, ItemDefDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCustomItemCommand(user.UserId(), req), ct)));

        group.MapPost("/heroic-abilities", async (CreateCustomHeroicAbilityRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCustomHeroicAbilityCommand, HeroicAbilityDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCustomHeroicAbilityCommand(user.UserId(), req), ct)));
    }
}
