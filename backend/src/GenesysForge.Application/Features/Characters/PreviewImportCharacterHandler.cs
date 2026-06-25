using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public class PreviewImportCharacterHandler(IAppDbContext db)
    : IQueryHandler<PreviewImportCharacterQuery, ImportPreviewDto>
{
    public async Task<ImportPreviewDto> Handle(PreviewImportCharacterQuery query, CancellationToken ct = default)
    {
        // Тот же резолвер, что и при импорте, но без сохранения — показываем сводку и предупреждения.
        var res = await CharacterImporter.ResolveAsync(db, query.UserId, query.Payload, ct);
        var c = res.Character;
        return new ImportPreviewDto(
            c.Name, c.System, res.ArchetypeName, res.CareerName,
            c.TotalXp, c.SpentXp,
            c.Skills.Count, c.Talents.Count, c.Items.Count, res.Notes.Count,
            res.Warnings);
    }
}
