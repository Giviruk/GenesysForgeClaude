using System.Security.Claims;
using GenesysForge.Api.Contracts;
using GenesysForge.Api.Data;
using GenesysForge.Api.Services;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? user.FindFirstValue("sub")
                   ?? throw new InvalidOperationException("Токен без идентификатора пользователя."));

    public static void MapReference(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reference").RequireAuthorization();

        // Справочник системы: встроенный контент + кастомный контент текущего пользователя.
        group.MapGet("/{system}", async (GameSystem system, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.UserId();

            var archetypes = await db.ArchetypeDefs.AsNoTracking()
                .Where(a => a.System == system).OrderBy(a => a.Name)
                .Select(a => CharacterService.ToDto(a)).ToListAsync();

            var careers = await db.CareerDefs.AsNoTracking()
                .Where(c => c.System == system).OrderBy(c => c.Name)
                .Select(c => new CareerDto(c.Id, c.Name, c.Description, c.CareerSkillNames)).ToListAsync();

            var skills = await db.SkillDefs.AsNoTracking()
                .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId))
                .OrderBy(s => s.Kind).ThenBy(s => s.Name)
                .Select(s => new SkillDefDto(s.Id, s.Name, s.Characteristic, s.Kind, s.OwnerUserId != null))
                .ToListAsync();

            var talents = await db.TalentDefs.AsNoTracking()
                .Where(t => t.System == system && (t.OwnerUserId == null || t.OwnerUserId == userId))
                .OrderBy(t => t.Tier).ThenBy(t => t.Name)
                .Select(t => new TalentDefDto(t.Id, t.Name, t.Tier, t.IsRanked, t.Activation, t.Description,
                    t.WoundBonus, t.StrainBonus, t.SoakBonus, t.MeleeDefenseBonus, t.RangedDefenseBonus,
                    t.OwnerUserId != null))
                .ToListAsync();

            var items = await db.ItemDefs.AsNoTracking()
                .Where(i => i.System == system && (i.OwnerUserId == null || i.OwnerUserId == userId))
                .OrderBy(i => i.Kind).ThenBy(i => i.Name)
                .Select(i => new ItemDefDto(i.Id, i.Name, i.Kind, i.Encumbrance, i.SoakBonus, i.MeleeDefense,
                    i.RangedDefense, i.EncumbranceThresholdBonus, i.Description, i.Price, i.Rarity,
                    i.OwnerUserId != null))
                .ToListAsync();

            var heroics = system == GameSystem.RealmsOfTerrinoth
                ? await db.HeroicAbilityDefs.AsNoTracking()
                    .Where(h => h.OwnerUserId == null || h.OwnerUserId == userId)
                    .OrderBy(h => h.Name)
                    .Select(h => new HeroicAbilityDto(h.Id, h.Name, h.Description, h.OwnerUserId != null))
                    .ToListAsync()
                : [];

            return Results.Ok(new ReferenceResponse(archetypes, careers, skills, talents, items, heroics));
        });
    }

    public static void MapCustomContent(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/custom").RequireAuthorization();

        group.MapPost("/skills", async (CreateCustomSkillRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new ErrorResponse("Название навыка не может быть пустым."));
            var userId = user.UserId();
            var name = req.Name.Trim();
            if (await db.SkillDefs.AnyAsync(s => s.System == req.System && s.Name == name
                    && (s.OwnerUserId == null || s.OwnerUserId == userId)))
                return Results.Conflict(new ErrorResponse("Навык с таким названием уже существует в этой системе."));

            var def = new SkillDef
            {
                Id = Guid.NewGuid(), System = req.System, Name = name,
                Characteristic = req.Characteristic, Kind = req.Kind, OwnerUserId = userId,
            };
            db.SkillDefs.Add(def);
            await db.SaveChangesAsync();
            return Results.Ok(new SkillDefDto(def.Id, def.Name, def.Characteristic, def.Kind, true));
        });

        group.MapPost("/talents", async (CreateCustomTalentRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new ErrorResponse("Название таланта не может быть пустым."));
            if (req.Tier is < 1 or > 5)
                return Results.BadRequest(new ErrorResponse("Тир таланта должен быть от 1 до 5."));

            var def = new TalentDef
            {
                Id = Guid.NewGuid(), System = req.System, Name = req.Name.Trim(), Tier = req.Tier,
                IsRanked = req.IsRanked, Activation = string.IsNullOrWhiteSpace(req.Activation) ? "Пассивный" : req.Activation.Trim(),
                Description = req.Description ?? "",
                WoundBonus = req.WoundBonus, StrainBonus = req.StrainBonus, SoakBonus = req.SoakBonus,
                MeleeDefenseBonus = req.MeleeDefenseBonus, RangedDefenseBonus = req.RangedDefenseBonus,
                OwnerUserId = user.UserId(),
            };
            db.TalentDefs.Add(def);
            await db.SaveChangesAsync();
            return Results.Ok(new TalentDefDto(def.Id, def.Name, def.Tier, def.IsRanked, def.Activation,
                def.Description, def.WoundBonus, def.StrainBonus, def.SoakBonus, def.MeleeDefenseBonus,
                def.RangedDefenseBonus, true));
        });

        group.MapPost("/items", async (CreateCustomItemRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new ErrorResponse("Название предмета не может быть пустым."));
            if (req.Encumbrance < 0)
                return Results.BadRequest(new ErrorResponse("Вес предмета не может быть отрицательным."));

            var def = new ItemDef
            {
                Id = Guid.NewGuid(), System = req.System, Name = req.Name.Trim(), Kind = req.Kind,
                Encumbrance = req.Encumbrance, SoakBonus = req.SoakBonus, MeleeDefense = req.MeleeDefense,
                RangedDefense = req.RangedDefense, EncumbranceThresholdBonus = req.EncumbranceThresholdBonus,
                Description = req.Description ?? "", Price = req.Price, Rarity = req.Rarity,
                OwnerUserId = user.UserId(),
            };
            db.ItemDefs.Add(def);
            await db.SaveChangesAsync();
            return Results.Ok(new ItemDefDto(def.Id, def.Name, def.Kind, def.Encumbrance, def.SoakBonus,
                def.MeleeDefense, def.RangedDefense, def.EncumbranceThresholdBonus, def.Description,
                def.Price, def.Rarity, true));
        });

        group.MapPost("/heroic-abilities", async (CreateCustomHeroicAbilityRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new ErrorResponse("Название способности не может быть пустым."));

            var def = new HeroicAbilityDef
            {
                Id = Guid.NewGuid(), Name = req.Name.Trim(), Description = req.Description ?? "",
                OwnerUserId = user.UserId(),
            };
            db.HeroicAbilityDefs.Add(def);
            await db.SaveChangesAsync();
            return Results.Ok(new HeroicAbilityDto(def.Id, def.Name, def.Description, true));
        });
    }
}
