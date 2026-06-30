using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class DuplicateCharacterHandler(IAppDbContext db) : ICommandHandler<DuplicateCharacterCommand, Guid>
{
    public async Task<Guid> Handle(DuplicateCharacterCommand command, CancellationToken ct = default)
    {
        var src = await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: false, ct);
        var now = DateTime.UtcNow;

        var copy = new Character
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.UserId,
            Name = $"{src.Name} (копия)",
            System = src.System,
            ArchetypeId = src.ArchetypeId,
            CareerId = src.CareerId,
            Brawn = src.Brawn,
            Agility = src.Agility,
            Intellect = src.Intellect,
            Cunning = src.Cunning,
            Willpower = src.Willpower,
            Presence = src.Presence,
            TotalXp = src.TotalXp,
            SpentXp = src.SpentXp,
            IsCreationPhase = src.IsCreationPhase,
            WoundsCurrent = src.WoundsCurrent,
            StrainCurrent = src.StrainCurrent,
            Money = src.Money,
            HeroicAbilityId = src.HeroicAbilityId,
            HeroicUpgradeRank = src.HeroicUpgradeRank,
            Desire = src.Desire,
            Fear = src.Fear,
            Strength = src.Strength,
            Flaw = src.Flaw,
            Background = src.Background,
            CreatedAt = now,
            Skills = src.Skills.Select(s => new CharacterSkill
            {
                SkillDefId = s.SkillDefId,
                Ranks = s.Ranks,
                IsCareer = s.IsCareer,
                FreeRanks = s.FreeRanks,
            }).ToList(),
            Talents = src.Talents.Select(t => new CharacterTalent
            {
                TalentDefId = t.TalentDefId,
                Ranks = t.Ranks,
                GrantedCharacteristics = t.GrantedCharacteristics,
            }).ToList(),
            Items = src.Items.Select(i => new CharacterItem
            {
                Id = Guid.NewGuid(),
                ItemDefId = i.ItemDefId,
                Quantity = i.Quantity,
                State = i.State,
            }).ToList(),
            CriticalInjuries = src.CriticalInjuries.Select(ci => new CharacterCriticalInjury
            {
                Id = Guid.NewGuid(),
                RuleCode = ci.RuleCode,
                NameRu = ci.NameRu,
                Severity = ci.Severity,
                RollResult = ci.RollResult,
                Notes = ci.Notes,
                CreatedAt = now,
            }).ToList(),
        };

        var notes = await db.CharacterNotes.AsNoTracking()
            .Where(n => n.CharacterId == src.Id)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);
        foreach (var note in notes)
        {
            db.CharacterNotes.Add(new CharacterNote
            {
                Id = Guid.NewGuid(),
                CharacterId = copy.Id,
                OwnerUserId = command.UserId,
                Title = note.Title,
                Body = note.Body,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        db.Characters.Add(copy);
        await db.SaveChangesAsync(ct);
        return copy.Id;
    }
}
