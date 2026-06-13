using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Notes;

namespace GenesysForge.Api.Endpoints;

public static class NoteEndpoints
{
    public static void MapNotes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/characters/{id:guid}/notes").RequireAuthorization();

        group.MapGet("/", async (Guid id, ClaimsPrincipal user,
                IQueryHandler<GetCharacterNotesQuery, List<CharacterNoteDto>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GetCharacterNotesQuery(user.UserId(), id), ct)));

        group.MapPost("/", async (Guid id, SaveCharacterNoteRequest req, ClaimsPrincipal user,
            ICommandHandler<CreateCharacterNoteCommand, CharacterNoteDto> handler, CancellationToken ct) =>
        {
            var note = await handler.Handle(new CreateCharacterNoteCommand(user.UserId(), id, req), ct);
            return Results.Created($"/api/characters/{id}/notes/{note.Id}", note);
        });

        group.MapPut("/{noteId:guid}", async (Guid id, Guid noteId, SaveCharacterNoteRequest req, ClaimsPrincipal user,
                ICommandHandler<UpdateCharacterNoteCommand, CharacterNoteDto> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new UpdateCharacterNoteCommand(user.UserId(), noteId, req), ct)));

        group.MapDelete("/{noteId:guid}", async (Guid id, Guid noteId, ClaimsPrincipal user,
            ICommandHandler<DeleteCharacterNoteCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new DeleteCharacterNoteCommand(user.UserId(), noteId), ct);
            return Results.NoContent();
        });
    }
}
