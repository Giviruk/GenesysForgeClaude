using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Npcs;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

public static class NpcEndpoints
{
    public static void MapNpcs(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/npcs").RequireAuthorization();

        // Enum-фильтры принимаем строками и парсим без учёта регистра: фронтенд шлёт
        // camelCase-значения (kind=minion), а биндинг enum в минимал-API регистрозависим.
        group.MapGet("/", async (ClaimsPrincipal user,
            IQueryHandler<GetNpcsQuery, List<NpcListItemDto>> handler, CancellationToken ct,
            string? search, string? system, string? kind, string? role,
            Guid? campaignId, string? tag, string? sort) =>
            Results.Ok(await handler.Handle(new GetNpcsQuery(
                user.UserId(), search,
                ParseEnum<GameSystem>(system, nameof(system)),
                ParseEnum<NpcKind>(kind, nameof(kind)),
                ParseEnum<NpcRole>(role, nameof(role)),
                campaignId, tag, sort), ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetNpcQuery, NpcDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetNpcQuery(user.UserId(), id), ct)));

        group.MapPost("/", async (NpcInput input, ClaimsPrincipal user,
            ICommandHandler<CreateNpcCommand, NpcDetailDto> handler, CancellationToken ct) =>
        {
            var npc = await handler.Handle(new CreateNpcCommand(user.UserId(), input), ct);
            return Results.Created($"/api/npcs/{npc.Id}", npc);
        });

        group.MapPost("/quick-draft", async (QuickDraftRequest req, ClaimsPrincipal user,
            ICommandHandler<QuickDraftNpcCommand, NpcDetailDto> handler, CancellationToken ct) =>
        {
            var npc = await handler.Handle(new QuickDraftNpcCommand(user.UserId(), req), ct);
            return Results.Created($"/api/npcs/{npc.Id}", npc);
        });

        group.MapPost("/apply-template", async (ApplyTemplateRequest req, ClaimsPrincipal user,
            ICommandHandler<ApplyNpcTemplateCommand, NpcDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ApplyNpcTemplateCommand(user.UserId(), req), ct)));

        group.MapPost("/{id:guid}/duplicate", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DuplicateNpcCommand, NpcDetailDto> handler, CancellationToken ct) =>
        {
            var npc = await handler.Handle(new DuplicateNpcCommand(user.UserId(), id), ct);
            return Results.Created($"/api/npcs/{npc.Id}", npc);
        });

        group.MapPut("/{id:guid}", async (Guid id, NpcInput input, ClaimsPrincipal user,
                ICommandHandler<UpdateNpcCommand, NpcDetailDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateNpcCommand(user.UserId(), id, input), ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user,
            ICommandHandler<DeleteNpcCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteNpcCommand(user.UserId(), id), ct);
            return Results.NoContent();
        });
    }

    private static T? ParseEnum<T>(string? value, string field) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<T>(value, ignoreCase: true, out var parsed)) return parsed;
        throw new DomainRuleException($"Недопустимое значение фильтра «{field}»: {value}.");
    }
}
