using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Reference;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static void MapReference(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reference").RequireAuthorization();

        // Справочник системы: встроенный контент + кастомный контент текущего пользователя.
        group.MapGet("/{system}", async (string system, ClaimsPrincipal user,
            IQueryHandler<GetReferenceQuery, ReferenceResponse> handler, CancellationToken ct) =>
        {
            // Биндинг enum из маршрута чувствителен к регистру — разбираем сами.
            if (!Enum.TryParse<GameSystem>(system, ignoreCase: true, out var gameSystem))
                throw new DomainRuleException($"Неизвестная система: «{system}».");
            return Results.Ok(await handler.Handle(new GetReferenceQuery(user.UserId(), gameSystem), ct));
        });

        // Справочные таблицы правил (системо-независимы; опц. ?q= для фильтра по подстроке).
        group.MapGet("/rules", async (string? q,
            IQueryHandler<GetRulesQuery, RulesResponse> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetRulesQuery(q), ct)));
    }
}
