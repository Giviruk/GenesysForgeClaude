using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Spells;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

public static class SpellEndpoints
{
    public static void MapSpells(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/spells").RequireAuthorization();

        group.MapGet("/{system}", async (string system, ClaimsPrincipal user,
            IQueryHandler<GetSpellsQuery, List<SpellDto>> handler, CancellationToken ct) =>
        {
            if (!Enum.TryParse<GameSystem>(system, ignoreCase: true, out var gameSystem))
                throw new DomainRuleException($"Неизвестная система: «{system}».");
            return Results.Ok(await handler.Handle(new GetSpellsQuery(user.UserId(), gameSystem), ct));
        });
    }
}
