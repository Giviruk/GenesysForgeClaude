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

        group.MapPost("/archetypes", async (CreateCustomArchetypeRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCustomArchetypeCommand, ArchetypeDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCustomArchetypeCommand(user.UserId(), req), ct)));

        group.MapPost("/careers", async (CreateCustomCareerRequest req, ClaimsPrincipal user,
                ICommandHandler<CreateCustomCareerCommand, CareerDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new CreateCustomCareerCommand(user.UserId(), req), ct)));

        // ---- Редактирование ----
        group.MapPut("/skills/{id:guid}", async (Guid id, CreateCustomSkillRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCustomSkillCommand, SkillDefDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCustomSkillCommand(user.UserId(), id, req), ct)));

        group.MapPut("/talents/{id:guid}", async (Guid id, CreateCustomTalentRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCustomTalentCommand, TalentDefDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCustomTalentCommand(user.UserId(), id, req), ct)));

        group.MapPut("/items/{id:guid}", async (Guid id, CreateCustomItemRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCustomItemCommand, ItemDefDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCustomItemCommand(user.UserId(), id, req), ct)));

        group.MapPut("/heroic-abilities/{id:guid}", async (Guid id, CreateCustomHeroicAbilityRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCustomHeroicAbilityCommand, HeroicAbilityDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCustomHeroicAbilityCommand(user.UserId(), id, req), ct)));

        group.MapPut("/archetypes/{id:guid}", async (Guid id, CreateCustomArchetypeRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCustomArchetypeCommand, ArchetypeDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCustomArchetypeCommand(user.UserId(), id, req), ct)));

        group.MapPut("/careers/{id:guid}", async (Guid id, CreateCustomCareerRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCustomCareerCommand, CareerDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCustomCareerCommand(user.UserId(), id, req), ct)));

        // ---- Удаление (блокируется, если контент используется персонажем) ----
        group.MapDelete("/skills/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCustomSkillCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCustomSkillCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapDelete("/talents/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCustomTalentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCustomTalentCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapDelete("/items/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCustomItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCustomItemCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapDelete("/heroic-abilities/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCustomHeroicAbilityCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCustomHeroicAbilityCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapDelete("/archetypes/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCustomArchetypeCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCustomArchetypeCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapDelete("/careers/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCustomCareerCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCustomCareerCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });
    }
}
