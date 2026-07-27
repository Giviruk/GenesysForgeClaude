using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class CreateCharacterHandler(IAppDbContext db, IDiceRoller dice)
    : ICommandHandler<CreateCharacterCommand, Guid>
{
    private const int MaxFreeCareerSkills = 4;

    public async Task<Guid> Handle(CreateCharacterCommand command, CancellationToken ct = default)
    {
        var (userId, req) = (command.UserId, command.Request);
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(db, userId, req.System, ct: ct);

        var archetype = await db.ArchetypeDefs
                .Include(a => a.StartingSkills)
                .Include(a => a.Abilities)
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
        // Retired-карьера остаётся у созданных персонажей, но новым не выдаётся: справочник её уже
        // не показывает, и присланный напрямую id тоже не должен проходить (ROT-CLEAN-3.1).
        if (career.Retired)
            throw new DomainRuleException(
                $"Карьера «{career.Name}» недоступна в этой системе.", "career.not_available_in_system");
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Имя персонажа не может быть пустым.");

        var freeSkills = req.FreeCareerSkillNames ?? [];
        if (freeSkills.Count > MaxFreeCareerSkills)
            throw new DomainRuleException($"При создании можно выбрать не более {MaxFreeCareerSkills} карьерных навыков для бесплатного ранга.");

        // Обязательный видовой выбор (Half-Catfolk) валидируется до создания сущности: пропустить
        // его, взять обе способности или указать чужой код нельзя, и подставлять умолчание тоже.
        var speciesChoice = ResolveSpeciesChoice(archetype, req.SpeciesAbilityChoiceCode);

        // Режим стартового снаряжения: отсутствие поля у старого клиента — безопасный StandardMoney.
        var mode = req.StartingEquipmentMode ?? StartingEquipmentMode.StandardMoney;
        var startingGear = await ResolveStartingGearAsync(career, req, mode, ct);

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
            // Бюджет покупок и карманные деньги — два разных счёта; складывать их нельзя.
            Money = startingGear.Money,
            SpeciesAbilityChoiceCode = speciesChoice,
            StartingEquipmentMode = mode,
            StartingPurchaseBudget = startingGear.PurchaseBudget,
            Desire = Clean(req.Desire),
            Fear = Clean(req.Fear),
            Strength = Clean(req.Strength),
            Flaw = Clean(req.Flaw),
            Background = Clean(req.Background),
        };

        // Резолвер навыков системы: built-in приоритетнее одноимённого custom.
        var systemSkills = await db.SkillDefs
            .Where(s => s.System == req.System && !s.Retired
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
        // Запись без имени группы отклоняется явной ошибкой: сгруппировать её не по чему, а
        // раньше такой запрос ронял создание необработанным исключением.
        if ((req.ArchetypeSkillChoices ?? []).Exists(c => string.IsNullOrWhiteSpace(c.ChoiceGroup)))
            throw new DomainRuleException(
                "У выбора стартовых навыков не указана группа.", "creation.skill_choice.group_missing");
        var providedChoices = (req.ArchetypeSkillChoices ?? [])
            .GroupBy(c => c.ChoiceGroup, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last().SkillNames ?? [], StringComparer.Ordinal);
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

        // Снаряжение уже разрешено и провалидировано до создания сущности — здесь только материализация.
        foreach (var line in startingGear.Items)
        {
            character.Items.Add(new CharacterItem
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                ItemDefId = line.ItemDefId,
                Quantity = line.Quantity,
                Provenance = ItemProvenance.CareerPackage,
            });
        }

        db.Characters.Add(character);

        CharacterAudit.Record(db, character, userId, CharacterAuditAction.CharacterCreated,
            $"Персонаж создан: {startingGear.Summary}", null,
            new
            {
                startingEquipmentMode = mode.ToString(),
                moneyFormula = startingGear.MoneyFormula,
                moneyRolled = startingGear.Money,
                purchaseBudget = startingGear.PurchaseBudget,
                packageItems = startingGear.Items.Count,
            });

        await db.SaveChangesAsync(ct);
        return character.Id;
    }

    /// <summary>
    /// Проверяет обязательный видовой выбор. Возвращает выбранный код или пустую строку, если
    /// вид выбора не требует. Некорректный запрос отклоняется с машинным <c>reasonCode</c>.
    /// </summary>
    private static string ResolveSpeciesChoice(ArchetypeDef archetype, string? requested)
    {
        var choice = archetype.Abilities
            .FirstOrDefault(a => a.RuleKind == SpeciesAbilityRuleKind.ChooseOneAbility);
        var code = requested?.Trim() ?? "";

        if (choice is null)
        {
            if (code.Length > 0)
                throw new DomainRuleException(
                    $"Вид {archetype.Name} не требует выбора видовой способности.",
                    "species.choice.not_applicable");
            return "";
        }

        var options = SpeciesAbilityRules.ChoiceOptions(choice);
        if (code.Length == 0)
            throw new DomainRuleException(
                $"Вид {archetype.Name} требует выбрать одну видовую способность: {string.Join(", ", options)}.",
                "species.choice.required");
        if (!options.Contains(code, StringComparer.Ordinal))
            throw new DomainRuleException(
                $"«{code}» не входит в список допустимых видовых способностей: {string.Join(", ", options)}.",
                "species.choice.unknown_option");
        return code;
    }

    /// <summary>Разрешённое стартовое снаряжение: деньги, бюджет и позиции комплекта.</summary>
    private sealed record StartingGearPlan(
        int Money, int PurchaseBudget, string MoneyFormula, string Summary,
        IReadOnlyList<(Guid ItemDefId, int Quantity)> Items);

    /// <summary>
    /// Полностью разрешает стартовое снаряжение до первой мутации (ROT-CRE-03). В режиме
    /// стандартных денег комплект не выдаётся и любые package choices — ошибка. В режиме комплекта
    /// требуется точное множество групп с ровно одной допустимой опцией каждая; бюджета 500 нет.
    /// </summary>
    private async Task<StartingGearPlan> ResolveStartingGearAsync(
        CareerDef career, CreateCharacterRequest req, StartingEquipmentMode mode, CancellationToken ct)
    {
        var requested = req.CareerGearChoices ?? [];

        if (mode == StartingEquipmentMode.StandardMoney)
        {
            if (requested.Count > 0)
                throw new DomainRuleException(
                    "В режиме стандартных денег карьерный комплект не выдаётся — выбор снаряжения недопустим.");

            var pocket = StartingWallet.PocketMoneyFormula;
            var rolled = pocket.Roll(dice.Roll);
            return new StartingGearPlan(rolled, StartingWallet.StandardPurchaseBudget, pocket.Describe(),
                $"бюджет {StartingWallet.StandardPurchaseBudget} и карманные {rolled} ({pocket.Describe()})",
                []);
        }

        var duplicates = requested
            .GroupBy(c => c.ChoiceGroup, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        var picks = requested
            .GroupBy(c => c.ChoiceGroup, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().OptionIndex, StringComparer.Ordinal);

        var (lines, error) = CareerPackageResolver.Resolve(career.StartingGear, picks, duplicates);
        if (error is not null) throw new DomainRuleException(error.Message, error.ReasonCode);

        // Все ItemDef комплекта обязаны резолвиться: молча пропустить позицию значит выдать
        // частичный комплект, что запрещено.
        var prefix = req.System == GameSystem.GenesysCore ? "gc" : "rot";
        var codes = lines!.Select(l => $"{prefix}.item.{l.ItemCode}").ToHashSet(StringComparer.Ordinal);
        var itemsByCode = await db.ItemDefs
            .Where(i => i.System == req.System && i.OwnerUserId == null && codes.Contains(i.Code))
            .ToDictionaryAsync(i => i.Code, ct);

        var resolved = new List<(Guid, int)>();
        foreach (var line in lines)
        {
            if (!itemsByCode.TryGetValue($"{prefix}.item.{line.ItemCode}", out var def))
                throw new DomainRuleException(
                    $"Предмет комплекта «{line.ItemNameFallback}» ({line.ItemCode}) не найден в каталоге системы.",
                    "career.package.item_unresolved");
            resolved.Add((def.Id, line.Quantity));
        }

        var formula = MoneyFormula.Parse(career.StartingMoneyFixed, career.StartingMoneyDice);
        var money = formula.Roll(dice.Roll);
        return new StartingGearPlan(money, 0, formula.Describe(),
            $"комплект карьеры {career.Name} ({resolved.Count} позиций) и {money} ({formula.Describe()})",
            resolved);
    }

    /// <summary>Нормализует опциональное текстовое поле: trim, пустое → null.</summary>
    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }


}
