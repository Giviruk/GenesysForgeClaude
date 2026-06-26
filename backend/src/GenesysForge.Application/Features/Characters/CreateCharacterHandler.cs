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
                .Include(a => a.StartingSkills)
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

        // Резолвер навыков системы: built-in приоритетнее одноимённого custom.
        var systemSkills = await db.SkillDefs
            .Where(s => s.System == req.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .ToListAsync(ct);
        var skillByName = systemSkills
            .OrderBy(s => s.OwnerUserId == null ? 0 : 1)
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => g.First());

        // Строки навыков создаются для карьерных и бесплатных рангов; остальные подмешиваются динамически.
        var charSkills = new Dictionary<Guid, CharacterSkill>();
        CharacterSkill GetOrCreate(SkillDef def, bool isCareer)
        {
            if (!charSkills.TryGetValue(def.Id, out var cs))
            {
                cs = new CharacterSkill
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    SkillDefId = def.Id,
                    IsCareer = isCareer,
                };
                charSkills[def.Id] = cs;
                character.Skills.Add(cs);
            }
            else if (isCareer) cs.IsCareer = true;
            return cs;
        }
        void AddFreeRanks(SkillDef def, int ranks)
        {
            var cs = GetOrCreate(def, isCareer: false);
            cs.Ranks += ranks;
            cs.FreeRanks += ranks;
        }

        foreach (var skill in systemSkills.Where(s => career.CareerSkillNames.Contains(s.Name)))
        {
            var cs = GetOrCreate(skill, isCareer: true);
            if (freeSkills.Contains(skill.Name)) { cs.Ranks += 1; cs.FreeRanks += 1; }
        }

        // Фиксированные стартовые навыки вида применяются автоматически (сливаясь с карьерными по рангам).
        foreach (var ss in archetype.StartingSkills.Where(s => !s.IsChoice))
        {
            if (string.IsNullOrWhiteSpace(ss.SkillName)) continue; // несопоставленный навык — пропускаем безопасно
            if (skillByName.TryGetValue(ss.SkillName, out var def))
                AddFreeRanks(def, ss.FreeRanks);
        }

        // Стартовые навыки-выборы вида — игрок выбирает конкретные навыки при создании.
        var providedChoices = (req.ArchetypeSkillChoices ?? [])
            .GroupBy(c => c.ChoiceGroup)
            .ToDictionary(g => g.Key, g => g.Last().SkillNames ?? []);
        foreach (var group in archetype.StartingSkills.Where(s => s.IsChoice))
        {
            if (!providedChoices.TryGetValue(group.ChoiceGroup, out var picks))
                throw new DomainRuleException($"Нужно выбрать {group.ChoiceCount} стартовых навыка вида.");
            var distinct = picks.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (distinct.Count != group.ChoiceCount)
                throw new DomainRuleException($"Нужно выбрать ровно {group.ChoiceCount} разных навыка вида.");
            foreach (var name in distinct)
            {
                if (!skillByName.TryGetValue(name, out var def))
                    throw new DomainRuleException($"Навык «{name}» не найден в системе.");
                if (group.ChoiceGroup == "any-noncareer" && career.CareerSkillNames.Contains(name))
                    throw new DomainRuleException($"«{name}» — карьерный навык; выберите некарьерный навык.");
                AddFreeRanks(def, group.FreeRanks);
            }
        }

        db.Characters.Add(character);
        await db.SaveChangesAsync(ct);
        return character.Id;
    }
}
