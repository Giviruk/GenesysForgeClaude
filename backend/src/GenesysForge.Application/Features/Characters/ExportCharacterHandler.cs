using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class ExportCharacterHandler(IAppDbContext db) : IQueryHandler<ExportCharacterQuery, CharacterExportDto>
{
    public async Task<CharacterExportDto> Handle(ExportCharacterQuery query, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(query.UserId, query.CharacterId, tracking: false, ct);

        var notes = await db.CharacterNotes
            .Where(n => n.CharacterId == c.Id)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new CharacterNoteExport(n.Title, n.Body))
            .ToListAsync(ct);

        var data = new CharacterExportData(
            Name: c.Name,
            System: c.System,
            ArchetypeCode: c.Archetype?.Code ?? "",
            ArchetypeName: c.Archetype?.Name ?? "",
            CareerCode: c.Career?.Code ?? "",
            CareerName: c.Career?.Name ?? "",
            Characteristics: new Dictionary<string, int>
            {
                ["brawn"] = c.Brawn,
                ["agility"] = c.Agility,
                ["intellect"] = c.Intellect,
                ["cunning"] = c.Cunning,
                ["willpower"] = c.Willpower,
                ["presence"] = c.Presence,
            },
            TotalXp: c.TotalXp,
            SpentXp: c.SpentXp,
            Money: c.Money,
            IsCreationPhase: c.IsCreationPhase,
            WoundsCurrent: c.WoundsCurrent,
            StrainCurrent: c.StrainCurrent,
            Skills: c.Skills
                .Select(s => new CharacterSkillExport(s.SkillDef?.Code ?? "", s.SkillDef?.Name ?? "", s.Ranks, s.IsCareer, s.FreeRanks))
                .ToList(),
            Talents: c.Talents
                .Select(t => new CharacterTalentExport(t.TalentDef?.Code ?? "", t.TalentDef?.Name ?? "", t.Ranks, t.GrantedCharacteristics))
                .ToList(),
            Items: c.Items
                .Select(i => new CharacterItemExport(i.ItemDef?.Code ?? "", i.ItemDef?.Name ?? "", i.Quantity, i.State))
                .ToList(),
            HeroicAbilityCode: c.HeroicAbility?.Code,
            HeroicAbilityName: c.HeroicAbility?.Name,
            HeroicUpgradeRank: c.HeroicUpgradeRank,
            Notes: notes);

        return new CharacterExportDto(CharacterExportDto.CurrentFormat, DateTime.UtcNow, data);
    }
}
