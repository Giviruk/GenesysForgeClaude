using System.Text.RegularExpressions;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public partial class CreateCharacterHandler(IAppDbContext db) : ICommandHandler<CreateCharacterCommand, Guid>
{
    private const int MaxFreeCareerSkills = 4;

    public async Task<Guid> Handle(CreateCharacterCommand command, CancellationToken ct = default)
    {
        var (userId, req) = (command.UserId, command.Request);

        var archetype = await db.ArchetypeDefs
                .Include(a => a.StartingSkills)
                .FirstOrDefaultAsync(a => a.Id == req.ArchetypeId && a.System == req.System
                    && !a.Retired && (a.OwnerUserId == null || a.OwnerUserId == userId), ct)
            ?? throw new DomainRuleException("Архетип не найден или принадлежит другой системе.");
        var career = await db.CareerDefs
                .Include(c => c.StartingGear)
                .FirstOrDefaultAsync(c => c.Id == req.CareerId && c.System == req.System
                    && (c.OwnerUserId == null || c.OwnerUserId == userId), ct)
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
            Money = career.StartingMoneyFixed + RollDice(career.StartingMoneyDice),
            Desire = Clean(req.Desire),
            Fear = Clean(req.Fear),
            Strength = Clean(req.Strength),
            Flaw = Clean(req.Flaw),
            Background = Clean(req.Background),
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

        // Стартовое снаряжение карьеры: фиксированное — автоматически, выборы — по запросу (лениво).
        await ApplyStartingGearAsync(character, career, req, ct);

        db.Characters.Add(character);
        await db.SaveChangesAsync(ct);
        return character.Id;
    }

    private async Task ApplyStartingGearAsync(Character character, CareerDef career, CreateCharacterRequest req, CancellationToken ct)
    {
        if (career.StartingGear.Count == 0) return;

        var prefix = req.System == GameSystem.GenesysCore ? "gc" : "rot";
        var codes = career.StartingGear.Where(g => g.ItemCode.Length > 0)
            .Select(g => $"{prefix}.item.{g.ItemCode}").ToHashSet();
        var itemsByCode = await db.ItemDefs
            .Where(i => i.System == req.System && i.OwnerUserId == null && codes.Contains(i.Code))
            .ToDictionaryAsync(i => i.Code, ct);

        var charItems = new Dictionary<Guid, CharacterItem>();
        void AddItem(string itemCode, int qty)
        {
            if (itemCode.Length == 0) return;
            if (!itemsByCode.TryGetValue($"{prefix}.item.{itemCode}", out var def)) return; // нерезолвленный — пропускаем
            if (!charItems.TryGetValue(def.Id, out var ci))
            {
                ci = new CharacterItem { Id = Guid.NewGuid(), CharacterId = character.Id, ItemDefId = def.Id, Quantity = 0 };
                charItems[def.Id] = ci;
                character.Items.Add(ci);
            }
            ci.Quantity += qty;
        }

        foreach (var g in career.StartingGear.Where(g => !g.IsChoice))
            AddItem(g.ItemCode, g.Quantity);

        var picks = (req.CareerGearChoices ?? [])
            .GroupBy(c => c.ChoiceGroup)
            .ToDictionary(g => g.Key, g => g.Last().OptionIndex);
        foreach (var group in career.StartingGear.Where(g => g.IsChoice).Select(g => g.ChoiceGroup).Distinct())
        {
            if (!picks.TryGetValue(group, out var optionIndex)) continue; // не выбран — снаряжение не обязательно
            var optionItems = career.StartingGear
                .Where(g => g.IsChoice && g.ChoiceGroup == group && g.ChoiceOption == optionIndex).ToList();
            if (optionItems.Count == 0)
                throw new DomainRuleException($"Неверный вариант стартового снаряжения для слота {group}.");
            foreach (var g in optionItems) AddItem(g.ItemCode, g.Quantity);
        }
    }

    /// <summary>Нормализует опциональное текстовое поле: trim, пустое → null.</summary>
    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>Бросок стартовых денег формата <c>NdM</c> (например «1d100»). Пусто/некорректно → 0.</summary>
    private static int RollDice(string dice)
    {
        if (string.IsNullOrWhiteSpace(dice)) return 0;
        var m = DiceRegex().Match(dice.Trim());
        if (!m.Success) return 0;
        var (count, sides) = (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
        var sum = 0;
        for (var i = 0; i < count; i++) sum += Random.Shared.Next(1, sides + 1);
        return sum;
    }

    [GeneratedRegex(@"^(\d+)d(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DiceRegex();
}
