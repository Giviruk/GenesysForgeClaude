using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Search;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearch(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search").RequireAuthorization();

        // Глобальный поиск: ?q= подстрока, ?system= система для контентных источников.
        group.MapGet("/", async (string? q, string? system, ClaimsPrincipal user,
            IQueryHandler<GlobalSearchQuery, SearchResponse> handler, CancellationToken ct) =>
        {
            if (!Enum.TryParse<GameSystem>(system, ignoreCase: true, out var gameSystem))
                throw new DomainRuleException($"Неизвестная система: «{system}».");
            return Results.Ok(await handler.Handle(
                new GlobalSearchQuery(user.UserId(), gameSystem, q ?? ""), ct));
        });
    }
}
