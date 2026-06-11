using System.Security.Claims;
using GenesysForge.Api.Contracts;
using GenesysForge.Api.Data;
using GenesysForge.Api.Services;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Endpoints;

public static class CharacterEndpoints
{
    public static void MapCharacters(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/characters").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.UserId();
            var list = await db.Characters.AsNoTracking()
                .Where(c => c.OwnerUserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CharacterListItemDto(c.Id, c.Name, c.System, c.Archetype!.Name, c.Career!.Name,
                    c.IsCreationPhase, c.CreatedAt))
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapPost("/", async (CreateCharacterRequest req, ClaimsPrincipal user, CharacterService svc) =>
        {
            var character = await svc.CreateAsync(user.UserId(), req);
            return Results.Created($"/api/characters/{character.Id}", new { character.Id });
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, CharacterService svc) =>
        {
            var userId = user.UserId();
            var character = await svc.GetOwnedAsync(userId, id, tracking: false);
            return Results.Ok(await svc.BuildSheetAsync(userId, character));
        });

        group.MapPatch("/{id:guid}", async (Guid id, UpdateCharacterRequest req, ClaimsPrincipal user,
            CharacterService svc, AppDbContext db) =>
        {
            var c = await svc.GetOwnedAsync(user.UserId(), id);
            if (req.Name is not null)
            {
                if (string.IsNullOrWhiteSpace(req.Name))
                    return Results.BadRequest(new ErrorResponse("Имя персонажа не может быть пустым."));
                c.Name = req.Name.Trim();
            }
            if (req.TotalXp is not null)
            {
                if (req.TotalXp < c.SpentXp)
                    return Results.BadRequest(new ErrorResponse($"Суммарный XP не может быть меньше потраченного ({c.SpentXp})."));
                c.TotalXp = req.TotalXp.Value;
            }
            if (req.WoundsCurrent is not null) c.WoundsCurrent = Math.Max(0, req.WoundsCurrent.Value);
            if (req.StrainCurrent is not null) c.StrainCurrent = Math.Max(0, req.StrainCurrent.Value);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, CharacterService svc, AppDbContext db) =>
        {
            var c = await svc.GetOwnedAsync(user.UserId(), id);
            db.Characters.Remove(c);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/complete-creation", async (Guid id, ClaimsPrincipal user, CharacterService svc,
            AppDbContext db) =>
        {
            var c = await svc.GetOwnedAsync(user.UserId(), id);
            c.IsCreationPhase = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/characteristics/{type}/buy", async (Guid id, CharacteristicType type,
            ClaimsPrincipal user, CharacterService svc) =>
        {
            await svc.BuyCharacteristicAsync(user.UserId(), id, type);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/skills/{skillDefId:guid}/buy-rank", async (Guid id, Guid skillDefId,
            ClaimsPrincipal user, CharacterService svc) =>
        {
            await svc.BuySkillRankAsync(user.UserId(), id, skillDefId);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/talents/buy", async (Guid id, BuyTalentRequest req, ClaimsPrincipal user,
            CharacterService svc) =>
        {
            await svc.BuyTalentAsync(user.UserId(), id, req.TalentDefId);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/heroic-ability", async (Guid id, SetHeroicAbilityRequest req, ClaimsPrincipal user,
            CharacterService svc) =>
        {
            await svc.SetHeroicAbilityAsync(user.UserId(), id, req.HeroicAbilityId);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/items", async (Guid id, AddItemRequest req, ClaimsPrincipal user,
            CharacterService svc) =>
        {
            var item = await svc.AddItemAsync(user.UserId(), id, req);
            return Results.Created($"/api/characters/{id}/items/{item.Id}", new { item.Id });
        });

        group.MapPatch("/{id:guid}/items/{itemId:guid}", async (Guid id, Guid itemId, UpdateItemRequest req,
            ClaimsPrincipal user, CharacterService svc) =>
        {
            await svc.UpdateItemAsync(user.UserId(), id, itemId, req);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}/items/{itemId:guid}", async (Guid id, Guid itemId, ClaimsPrincipal user,
            CharacterService svc) =>
        {
            await svc.RemoveItemAsync(user.UserId(), id, itemId);
            return Results.NoContent();
        });
    }
}
