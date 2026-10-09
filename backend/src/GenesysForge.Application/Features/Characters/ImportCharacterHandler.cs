using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

public class ImportCharacterHandler(IAppDbContext db) : ICommandHandler<ImportCharacterCommand, ImportCharacterResult>
{
    public async Task<ImportCharacterResult> Handle(ImportCharacterCommand command, CancellationToken ct = default)
    {
        // Импорт всегда создаёт нового персонажа — существующего не перезаписываем.
        var res = await CharacterImporter.ResolveAsync(db, command.UserId, command.Payload, ct, command.CampaignId);

        db.Characters.Add(res.Character);
        foreach (var note in res.Notes)
            db.CharacterNotes.Add(note);
        if (command.CampaignId is { } cid)
        {
            var campaign = await CampaignMapper.GetAccessibleAsync(db, command.UserId, cid, ct);
            await CampaignMembership.EnsureMemberAsync(db, campaign, command.UserId, ct);
            db.CampaignCharacters.Add(new CampaignCharacter { Id = Guid.NewGuid(), CampaignId = cid, CharacterId = res.Character.Id, PlayerUserId = command.UserId });
        }
        await db.SaveChangesAsync(ct);

        return new ImportCharacterResult(res.Character.Id, res.Character.Name, res.Warnings);
    }
}
