using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

public static class CharacterEndpoints
{
    public static void MapCharacters(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/characters").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user,
                IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCharactersQuery(user.UserId()), ct)));

        group.MapPost("/", async (CreateCharacterRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateCharacterCommand, Guid> handler, CancellationToken ct) =>
        {
            var id = await handler.Handle(new CreateCharacterCommand(user.UserId(), req), ct);
            return Results.Created($"/api/characters/{id}", new { Id = id });
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetCharacterSheetQuery, CharacterSheetDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCharacterSheetQuery(user.UserId(), id), ct)));

        group.MapPatch("/{id:guid}", async (Guid id, UpdateCharacterRequest req, ClaimsPrincipal user,
            ICommandHandler<UpdateCharacterCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new UpdateCharacterCommand(user.UserId(), id, req), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteCharacterCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCharacterCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/complete-creation", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<CompleteCreationCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new CompleteCreationCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/characteristics/{type}/buy", async (Guid id, string type,
            ClaimsPrincipal user, ICommandHandler<BuyCharacteristicCommand, Unit> handler, CancellationToken ct) =>
        {
            // Биндинг enum из маршрута чувствителен к регистру, а фронтенд шлёт camelCase — разбираем сами.
            if (!Enum.TryParse<CharacteristicType>(type, ignoreCase: true, out var characteristic))
                throw new DomainRuleException($"Неизвестная характеристика: «{type}».");
            await handler.Handle(new BuyCharacteristicCommand(user.UserId(), id, characteristic), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/skills/{skillDefId:guid}/buy-rank", async (Guid id, Guid skillDefId,
            ClaimsPrincipal user, ICommandHandler<BuySkillRankCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new BuySkillRankCommand(user.UserId(), id, skillDefId), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/talents/buy", async (Guid id, BuyTalentRequest req, ClaimsPrincipal user,
            ICommandHandler<BuyTalentCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new BuyTalentCommand(user.UserId(), id, req.TalentDefId), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-ability", async (Guid id, SetHeroicAbilityRequest req, ClaimsPrincipal user,
            ICommandHandler<SetHeroicAbilityCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new SetHeroicAbilityCommand(user.UserId(), id, req.HeroicAbilityId), ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/items", async (Guid id, AddItemRequest req, ClaimsPrincipal user,
            ICommandHandler<AddItemCommand, Guid> handler, CancellationToken ct) =>
        {
            var itemId = await handler.Handle(new AddItemCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/items/{itemId}", new { Id = itemId });
        });

        group.MapPatch("/{id:guid}/items/{itemId:guid}", async (Guid id, Guid itemId, UpdateItemRequest req,
            ClaimsPrincipal user, ICommandHandler<UpdateItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new UpdateItemCommand(user.UserId(), id, itemId, req), ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}/items/{itemId:guid}", async (Guid id, Guid itemId, ClaimsPrincipal user,
            ICommandHandler<RemoveItemCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RemoveItemCommand(user.UserId(), id, itemId), ct);
            return Results.NoContent();
        });
    }
}
