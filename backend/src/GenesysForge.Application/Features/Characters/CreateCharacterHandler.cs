using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class CreateCharacterHandler(IAppDbContext db) : ICommandHandler<CreateCharacterCommand, Guid>
{
    private const int MaxFreeCareerSkills = 4;

    public async Task<Guid> Handle(CreateCharacterCommand command, CancellationToken ct = default)
    {
        var (userId, req) = (command.UserId, command.Request);

        var archetype = await db.ArchetypeDefs
                .FirstOrDefaultAsync(a => a.Id == req.ArchetypeId && a.System == req.System, ct)
            ?? throw new DomainRuleException("Архетип не найден или принадлежит другой системе.");
        var career = await db.CareerDefs
                .FirstOrDefaultAsync(c => c.Id == req.CareerId && c.System == req.System, ct)
            ?? throw new DomainRuleException("Карьера не найдена или принадлежит другой системе.");
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Имя персонажа не может быть пустым.");

        var freeSkills = req.FreeCareerSkillNames ?? [];
        if (freeSkills.Count > MaxFreeCareerSkills)
            throw new DomainRuleException($"При создании можно выбрать не более {MaxFreeCareerSkills} карьерных навыков для бесплатного ранга.");
        var invalid = freeSkills.FirstOrDefault(n => !career.CareerSkillNames.Contains(n));
        if (invalid is not null)
            throw new DomainRuleException($"«{invalid}» не является карьерным навыком карьеры {career.Name}.");

        var character = new Character
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = req.Name.Trim(),
            System = req.System,
            ArchetypeId = archetype.Id,
            CareerId = career.Id,
            Brawn = archetype.Brawn,
            Agility = archetype.Agility,
            Intellect = archetype.Intellect,
            Cunning = archetype.Cunning,
            Willpower = archetype.Willpower,
            Presence = archetype.Presence,
            TotalXp = archetype.StartingXp,
        };

        // Строки навыков создаются для карьерных и бесплатных рангов; остальные подмешиваются динамически.
        var systemSkills = await db.SkillDefs
            .Where(s => s.System == req.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .ToListAsync(ct);
        foreach (var skill in systemSkills.Where(s => career.CareerSkillNames.Contains(s.Name)))
        {
            character.Skills.Add(new CharacterSkill
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                SkillDefId = skill.Id,
                IsCareer = true,
                Ranks = freeSkills.Contains(skill.Name) ? 1 : 0,
            });
        }

        db.Characters.Add(character);
        await db.SaveChangesAsync(ct);
        return character.Id;
    }
}
