using System.Text.RegularExpressions;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public partial class CreateCharacterHandler(IAppDbContext db) : ICommandHandler<CreateCharacterCommand, Guid>
{
    private const int MaxFreeCareerSkills = 4;

    public async Task<Guid> Handle(CreateCharacterCommand command, CancellationToken ct = default)
    {
        var (userId, req) = (command.UserId, command.Request);
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(db, userId, req.System, ct: ct);

        var archetype = await db.ArchetypeDefs
                .Include(a => a.StartingSkills)
                .FirstOrDefaultAsync(a => a.Id == req.ArchetypeId && a.System == req.System
                    && !a.Retired
                    && (a.OwnerUserId == null
                        || (a.OwnerUserId == userId
                            && (a.HomebrewPackId == null || visiblePackIds.Contains(a.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Архетип не найден или принадлежит другой системе.");
        var career = await db.CareerDefs
                .Include(c => c.StartingGear)
                .FirstOrDefaultAsync(c => c.Id == req.CareerId && c.System == req.System
                    && (c.OwnerUserId == null
                        || (c.OwnerUserId == userId
                            && (c.HomebrewPackId == null || visiblePackIds.Contains(c.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Карьера не найдена или принадлежит другой системе.");
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Имя персонажа не может быть пустым.");

        var freeSkills = req.FreeCareerSkillNames ?? [];
        if (freeSkills.Count > MaxFreeCareerSkills)
            throw new DomainRuleException($"При создании можно выбрать не более {MaxFreeCareerSkills} карьерных навыков для бесплатного ранга.");

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
            .Where(s => s.System == req.System
                && (s.OwnerUserId == null
                    || (s.OwnerUserId == userId
                        && (s.HomebrewPackId == null || visiblePackIds.Contains(s.HomebrewPackId.Value)))))
            .ToListAsync(ct);
        var skillByName = CareerSkills.BuildNameIndex(systemSkills);

        // Карьерный статус резолвится один раз из всех источников: карьера ∪ вид ∪ таланты.
        // На создании талантов ещё нет, поэтому источников два.
        var careerSkills = CareerSkillResolver.Resolve(
            CareerSkills.GrantsFor(career, archetype, []),
            name => skillByName.TryGetValue(name, out var def) ? def.Id : null);

        // Полный план бесплатных рангов строится целиком до первой записи и только потом проверяется:
        // превышение предела создания — ошибка, обрезать ранг нельзя.
        var plan = new CreationSkillPlan();
        foreach (var skillDefId in careerSkills.SkillDefIds)
        {
            var def = systemSkills.First(s => s.Id == skillDefId);
            plan.MarkCareer(def.Id, def.Name);
        }

        var invalidFree = freeSkills.FirstOrDefault(n =>
            !skillByName.TryGetValue(n, out var def) || !careerSkills.IsCareer(def.Id));
        if (invalidFree is not null)
            throw new DomainRuleException($"«{invalidFree}» не является карьерным навыком карьеры {career.Name}.");
        foreach (var name in freeSkills.Distinct(StringComparer.Ordinal))
            plan.AddFreeRanks(skillByName[name].Id, name, 1, $"карьера {career.Name}");

        // Фиксированные стартовые навыки вида применяются автоматически (сливаясь с карьерными по рангам).
        foreach (var ss in archetype.StartingSkills.Where(s => !s.IsChoice))
        {
            if (string.IsNullOrWhiteSpace(ss.SkillName)) continue; // несопоставленный навык — пропускаем безопасно
            if (skillByName.TryGetValue(ss.SkillName, out var def))
                plan.AddFreeRanks(def.Id, def.Name, ss.FreeRanks, $"вид {archetype.Name}");
        }

        // Стартовые навыки-выборы вида — игрок выбирает конкретные навыки при создании.
        var providedChoices = (req.ArchetypeSkillChoices ?? [])
            .GroupBy(c => c.ChoiceGroup)
            .ToDictionary(g => g.Key, g => g.Last().SkillNames ?? []);
        foreach (var group in archetype.StartingSkills.Where(s => s.IsChoice))
        {
            if (!providedChoices.TryGetValue(group.ChoiceGroup, out var picks))
                throw new DomainRuleException($"Нужно выбрать {group.ChoiceCount} стартовых навыка вида.");
            var distinct = picks.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.Ordinal).ToList();
            if (distinct.Count != group.ChoiceCount)
                throw new DomainRuleException($"Нужно выбрать ровно {group.ChoiceCount} разных навыка вида.");
            foreach (var name in distinct)
            {
                if (!skillByName.TryGetValue(name, out var def))
                    throw new DomainRuleException($"Навык «{name}» не найден в системе.");
                if (group.ChoiceGroup == "any-noncareer" && careerSkills.IsCareer(def.Id))
                    throw new DomainRuleException($"«{name}» — карьерный навык; выберите некарьерный навык.");
                plan.AddFreeRanks(def.Id, def.Name, group.FreeRanks, $"выбор вида {archetype.Name}");
            }
        }

        var violations = plan.Validate();
        if (violations.Count > 0)
            throw new DomainRuleException(
                "Стартовые ранги превышают предел создания. " + string.Join(" ", violations.Select(v => v.Describe())));

        // План валиден — материализуем строки навыков. Остальные навыки подмешиваются динамически.
        foreach (var entry in plan.Entries)
        {
            character.Skills.Add(new CharacterSkill
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                SkillDefId = entry.SkillDefId,
                IsCareer = entry.IsCareer,
                Ranks = entry.TotalRanks,
                FreeRanks = entry.TotalRanks,
            });
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
