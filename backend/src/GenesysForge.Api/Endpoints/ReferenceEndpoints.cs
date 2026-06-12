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
        group.MapGet("/{system}", async (GameSystem system, ClaimsPrincipal user,
                IQueryHandler<GetReferenceQuery, ReferenceResponse> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetReferenceQuery(user.UserId(), system), ct)));
    }
}
