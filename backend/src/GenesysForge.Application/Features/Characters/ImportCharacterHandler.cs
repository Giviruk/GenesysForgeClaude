using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public class ImportCharacterHandler(IAppDbContext db) : ICommandHandler<ImportCharacterCommand, ImportCharacterResult>
{
    public async Task<ImportCharacterResult> Handle(ImportCharacterCommand command, CancellationToken ct = default)
    {
        // Импорт всегда создаёт нового персонажа — существующего не перезаписываем.
        var res = await CharacterImporter.ResolveAsync(db, command.UserId, command.Payload, ct);

        db.Characters.Add(res.Character);
        foreach (var note in res.Notes)
            db.CharacterNotes.Add(note);
        await db.SaveChangesAsync(ct);

        return new ImportCharacterResult(res.Character.Id, res.Character.Name, res.Warnings);
    }
}
