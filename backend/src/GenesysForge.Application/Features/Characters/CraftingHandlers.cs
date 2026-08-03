using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Общая часть изготовления, варки и зачарования (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01):
/// цель, навык, сложность, время и стоимость компонентов.
/// </summary>
/// <remarks>
/// Требования по ресурсам — инструменты, компоненты, ингредиенты — по решению владельца остаются
/// описанием: наличие не проверяется и ничего не списывается. Стоимость считается и записывается,
/// но кошелька не касается, поэтому проект нельзя «не потянуть».
/// </remarks>
public static class CraftingCalc
{
    /// <summary>Рекомендованная сложность зачарования: для большинства магических вещей Formidable.</summary>
    public const int EnchantmentDifficulty = 5;

    /// <summary>Навык по умолчанию: Механика, Выживание у грубой работы, Алхимия у варки.</summary>
    public static string DefaultSkill(CraftingKind kind, bool roughSurvival) => kind switch
    {
        CraftingKind.Potion => "Alchemy",
        CraftingKind.Enchantment => "Arcana",
        _ => roughSurvival ? "Survival" : "Mechanics",
    };

    /// <summary>Единица времени: у варки одной партии это часы, у остального дни.</summary>
    public static string TimeUnit(CraftingKind kind) => kind == CraftingKind.Potion ? "hours" : "days";

    /// <summary>Целевая запись каталога, видимая этому персонажу.</summary>
    public static async Task<ItemDef> TargetAsync(
        IAppDbContext db, Guid userId, Character c, Guid itemDefId, CancellationToken ct)
    {
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, userId, c.System, c.Id, ct: ct);
        return await db.ItemDefs.FirstOrDefaultAsync(i =>
                i.Id == itemDefId && i.System == c.System
                && (i.OwnerUserId == null
                    || (i.OwnerUserId == userId
                        && (i.HomebrewPackId == null || visiblePackIds.Contains(i.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Предмет не найден.", "crafting.target_not_found");
    }

    /// <summary>Числа проекта по правилам и поправкам запроса. Ничего не пишет.</summary>
    public static CraftingNumbers Compute(ItemDef def, CraftingProjectInput req)
    {
        var rarity = def.Rarity ?? 0;
        var baseDifficulty = req.Kind == CraftingKind.Enchantment
            ? EnchantmentDifficulty
            : CraftingRules.Difficulty(rarity);
        var baseTime = CraftingRules.BaseTime(rarity);
        // У зачарования рецепта нет: и стоимость компонентов, и время назначает ведущий явно.
        var listedCost = req.Kind == CraftingKind.Enchantment
            ? 0
            : CraftingRules.ComponentCost(def.Price ?? 0);

        var difficulty = baseDifficulty;
        if (req.DifficultyOverride is { } d)
        {
            if (d is < 0 or > 5)
                throw new DomainRuleException(
                    "Сложность задаётся от 0 до 5.", "crafting.difficulty_invalid");
            if (string.IsNullOrWhiteSpace(req.DifficultyReason))
                throw new DomainRuleException(
                    "Для изменённой сложности нужна причина.", "crafting.difficulty_reason_required");
            difficulty = d;
        }

        var time = baseTime;
        if (req.TimeOverride is { } tv)
        {
            if (tv < 1)
                throw new DomainRuleException(
                    "Время работы не меньше одной единицы.", "crafting.time_invalid");
            if (string.IsNullOrWhiteSpace(req.TimeReason))
                throw new DomainRuleException(
                    "Для изменённого времени нужна причина.", "crafting.time_reason_required");
            time = tv;
        }

        var cost = CraftingRules.Cost(listedCost, req.CostPercent, req.CostOverride, req.CostOverrideReason);
        return new CraftingNumbers(baseDifficulty, difficulty, baseTime, time, listedCost, cost);
    }

    public sealed record CraftingNumbers(
        int BaseDifficulty, int Difficulty, int BaseTime, int Time, int ListedCost, int Cost);
}

/// <summary>Предпросмотр проекта: те же числа, что при создании, но без записи в базу.</summary>
public class PreviewCraftingHandler(IAppDbContext db) : IQueryHandler<PreviewCraftingQuery, CraftingPreviewDto>
{
    public async Task<CraftingPreviewDto> Handle(PreviewCraftingQuery q, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(q.UserId, q.CharacterId, ct: ct);
        var def = await CraftingCalc.TargetAsync(db, q.UserId, c, q.Request.ItemDefId, ct);
        CraftingRules.EnsureCraftable(def, q.Request.Kind);

        var n = CraftingCalc.Compute(def, q.Request);
        var table = q.Request.Kind == CraftingKind.Potion ? CraftingKind.Potion : CraftingKind.Item;
        var spends = await db.CraftingSpendDefs.AsNoTracking()
            .Where(s => s.Table == table && !s.Retired)
            .OrderBy(s => s.SortOrder).ToListAsync(ct);

        return new CraftingPreviewDto(
            q.Request.Kind, def.Name, def.Price, def.Rarity,
            string.IsNullOrWhiteSpace(q.Request.SkillName)
                ? CraftingCalc.DefaultSkill(q.Request.Kind, q.Request.RoughSurvival)
                : q.Request.SkillName!.Trim(),
            n.BaseDifficulty, n.Difficulty, n.BaseTime, n.Time, CraftingCalc.TimeUnit(q.Request.Kind),
            n.ListedCost, q.Request.CostPercent, q.Request.CostOverride, n.Cost,
            def.Kind == ItemKind.Weapon,
            [.. spends.Select(CraftingMapper.ToDto)]);
    }
}

/// <summary>Начало проекта. Денег не списывает и наличия материалов не проверяет.</summary>
public class StartCraftingHandler(IAppDbContext db) : ICommandHandler<StartCraftingCommand, Guid>
{
    public async Task<Guid> Handle(StartCraftingCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var def = await CraftingCalc.TargetAsync(db, command.UserId, c, req.ItemDefId, ct);
        CraftingRules.EnsureCraftable(def, req.Kind);

        if (req.Kind == CraftingKind.Enchantment)
        {
            if (req.BaseCharacterItemId is not { } baseId)
                throw new DomainRuleException(
                    "Зачарование начинается с конкретной вещи в инвентаре.", "crafting.base_required");
            var baseItem = c.Items.FirstOrDefault(i => i.Id == baseId)
                ?? throw new DomainRuleException("Основа не найдена в инвентаре.", "crafting.base_not_found");
            if (baseItem.ItemDefId != def.Id)
                throw new DomainRuleException(
                    "Основа и выбранная запись каталога не совпадают.", "crafting.base_mismatch");
            CraftingRules.EnsureEnchantable(EffectiveItems.For(c, baseItem).Qualities);
            if (string.IsNullOrWhiteSpace(req.Intent))
                throw new DomainRuleException(
                    "Для зачарования нужно заранее описать согласованную способность.",
                    "crafting.intent_required");
        }

        var n = CraftingCalc.Compute(def, req);
        var project = new CraftingProject
        {
            Id = Guid.NewGuid(),
            CharacterId = c.Id,
            Kind = req.Kind,
            Status = CraftingProjectStatus.Draft,
            ItemDefId = def.Id,
            BaseCharacterItemId = req.Kind == CraftingKind.Enchantment ? req.BaseCharacterItemId : null,
            TargetName = def.Name,
            TargetPrice = def.Price,
            TargetRarity = def.Rarity,
            SkillName = string.IsNullOrWhiteSpace(req.SkillName)
                ? CraftingCalc.DefaultSkill(req.Kind, req.RoughSurvival)
                : req.SkillName!.Trim(),
            BaseDifficulty = n.BaseDifficulty,
            Difficulty = n.Difficulty,
            DifficultyReason = req.DifficultyReason?.Trim() ?? "",
            BaseTime = n.BaseTime,
            Time = n.Time,
            TimeReason = req.TimeReason?.Trim() ?? "",
            ListedCost = n.ListedCost,
            CostPercent = req.CostOverride is null ? req.CostPercent : 100,
            CostOverride = req.CostOverride,
            CostOverrideReason = req.CostOverrideReason?.Trim() ?? "",
            Cost = n.Cost,
            Requirements = req.Requirements?.Trim() ?? "",
            Intent = req.Intent?.Trim() ?? "",
            RoughSurvival = req.RoughSurvival && req.Kind == CraftingKind.Item,
        };
        db.CraftingProjects.Add(project);

        var unit = CraftingCalc.TimeUnit(req.Kind) == "hours" ? "ч" : "дн";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.CraftingStarted,
            $"Начат проект «{def.Name}»: сложность {project.Difficulty}, {project.Time} {unit}, "
            + $"компоненты {project.Cost}", null,
            new
            {
                kind = project.Kind.ToString(), target = def.Name, skill = project.SkillName,
                baseDifficulty = project.BaseDifficulty, difficulty = project.Difficulty,
                difficultyReason = project.DifficultyReason,
                baseTime = project.BaseTime, time = project.Time, timeReason = project.TimeReason,
                listedCost = project.ListedCost, percent = project.CostPercent,
                costOverride = project.CostOverride, costOverrideReason = project.CostOverrideReason,
                cost = project.Cost,
                mode = project.CostOverride is not null ? "override"
                    : project.CostPercent != 100 ? "haggle" : "direct",
                requirements = project.Requirements, intent = project.Intent,
                roughSurvival = project.RoughSurvival,
            });

        await db.SaveChangesAsync(ct);
        return project.Id;
    }
}

/// <summary>
/// Разрешение проекта. Символы броска присылает клиент из роллера — та же конвенция, что у продажи
/// по проверке (ROT-ECO-01); всё остальное считает сервер по кодам таблицы. Повторно разрешить
/// проект нельзя: второй экземпляр из одного броска не появляется.
/// </summary>
public class ResolveCraftingHandler(IAppDbContext db)
    : ICommandHandler<ResolveCraftingCommand, CraftingProjectDto>
{
    public async Task<CraftingProjectDto> Handle(ResolveCraftingCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var project = await db.CraftingProjects
                .Include(p => p.Spends)
                .FirstOrDefaultAsync(p => p.Id == command.ProjectId && p.CharacterId == c.Id, ct)
            ?? throw new DomainRuleException("Проект не найден.", "crafting.project_not_found");
        if (project.Status != CraftingProjectStatus.Draft)
            throw new DomainRuleException(
                "Проект уже разрешён или отменён.", "crafting.project_not_draft");

        if (req.Advantages < 0 || req.Threats < 0 || req.Triumphs < 0 || req.Despairs < 0)
            throw new DomainRuleException("Символов не бывает меньше нуля.", "crafting.symbols_invalid");

        var def = await db.ItemDefs.Include(i => i.Qualities).ThenInclude(q => q.QualityDef)
                .FirstOrDefaultAsync(i => i.Id == project.ItemDefId, ct)
            ?? throw new DomainRuleException("Запись каталога исчезла.", "crafting.target_not_found");

        var table = project.Kind == CraftingKind.Potion ? CraftingKind.Potion : CraftingKind.Item;
        var catalog = await db.CraftingSpendDefs.AsNoTracking()
            .Where(s => s.Table == table && !s.Retired).ToDictionaryAsync(s => s.Code, ct);

        var choices = (req.Spends ?? [])
            .Select(s => new CraftingSpendChoice(s.Code, s.Count, s.Parameter ?? "", s.PaidWith))
            .ToList();
        await EnsureCombineTargetsAsync(db, choices, catalog, def, ct);

        var success = req.NetSuccesses > 0;
        var outcome = CraftingRules.Allocate(
            choices, catalog, req.Advantages, req.Threats, req.Triumphs, req.Despairs,
            project.Time, def.Kind == ItemKind.Weapon, success);

        project.NetSuccesses = req.NetSuccesses;
        project.Advantages = req.Advantages;
        project.Threats = req.Threats;
        project.Triumphs = req.Triumphs;
        project.Despairs = req.Despairs;
        project.Time = outcome.Time;
        project.Status = CraftingProjectStatus.Resolved;
        project.ResolvedAt = DateTime.UtcNow;
        foreach (var ch in choices)
        {
            var spendDef = catalog[ch.Code];
            // Ключ оставлен генератору: трата попадает в контекст через навигацию отслеживаемого
            // проекта, и заранее проставленный Guid читался бы как правка несуществующей строки.
            var spend = new CraftingProjectSpend
            {
                CraftingProjectId = project.Id,
                SpendCode = ch.Code, Count = ch.Count, Parameter = ch.Parameter.Trim(),
                PaidWith = ch.PaidWith.ToLowerInvariant(),
                TextRu = spendDef.NameRu, TextEn = spendDef.Name,
            };
            // Проект отслеживается, поэтому добавления в навигацию достаточно: второй Add в набор
            // положил бы ту же трату в список дважды.
            project.Spends.Add(spend);
        }

        // Описание результата — то же, что попадает в предмет: игрок должен видеть каждый свой
        // выбор словами, потому что половина трат приложением не исполняется.
        var lines = new List<string> { $"Изготовлено персонажем: {project.TargetName}." };
        if (project.RoughSurvival) lines.Add("Грубая работа Выживанием: ведущий может сломать её на отчаянии.");
        if (!string.IsNullOrWhiteSpace(project.Intent)) lines.Add($"Замысел: {project.Intent}");
        lines.AddRange(outcome.Notes);
        var note = string.Join("\n", lines);
        project.Outcome = success ? note : "Провал: предмет не создан.\n" + string.Join("\n", outcome.Notes);

        if (success && project.Kind == CraftingKind.Enchantment)
        {
            // Зачарование не создаёт вещь, а меняет уже существующую: способность дописывается
            // основе, и она же остаётся результатом проекта.
            var baseItem = c.Items.FirstOrDefault(i => i.Id == project.BaseCharacterItemId)
                ?? throw new DomainRuleException("Основа не найдена в инвентаре.", "crafting.base_not_found");
            baseItem.CraftingProjectId = project.Id;
            baseItem.CraftNote = string.IsNullOrWhiteSpace(baseItem.CraftNote)
                ? $"Зачаровано персонажем: {project.Intent}\n" + string.Join("\n", outcome.Notes)
                : baseItem.CraftNote + "\n" + $"Зачаровано персонажем: {project.Intent}";
            baseItem.CraftedEncumbrance += outcome.EncumbranceDelta;
            baseItem.CraftedHardPoints += outcome.HardPointsDelta;
            baseItem.CraftedQualities = CraftingRules.PackQualities(
                CraftingRules.UnpackQualities(baseItem.CraftedQualities).Concat(outcome.Qualities)
                    .GroupBy(q => q.Code, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new EffectiveQuality(g.First().Code, g.Sum(x => x.Rating))));
            baseItem.CraftedFragile |= outcome.Fragile;
            project.CreatedCharacterItemId = baseItem.Id;
        }
        else if (success)
        {
            var item = new CharacterItem
            {
                Id = Guid.NewGuid(),
                CharacterId = c.Id,
                ItemDefId = def.Id,
                ItemDef = def,
                Quantity = Math.Max(1, outcome.Quantity),
                State = ItemState.Carried,
                // Метка «создано персонажем» живёт в происхождении позиции, а не в её названии.
                Provenance = project.RoughSurvival ? ItemProvenance.RoughSurvival : ItemProvenance.Crafted,
                CraftingProjectId = project.Id,
                CraftedEncumbrance = outcome.EncumbranceDelta,
                CraftedHardPoints = outcome.HardPointsDelta,
                CraftedQualities = CraftingRules.PackQualities(outcome.Qualities),
                CraftedFragile = outcome.Fragile,
                CraftNote = note,
            };
            db.CharacterItems.Add(item);
            c.Items.Add(item);
            project.CreatedCharacterItemId = item.Id;
        }

        var verdict = success ? "успех" : "провал";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.CraftingResolved,
            $"Проект «{project.TargetName}» — {verdict}"
            + (success ? $", создано ×{Math.Max(1, outcome.Quantity)}" : ""), null,
            new
            {
                kind = project.Kind.ToString(), target = project.TargetName,
                netSuccesses = req.NetSuccesses, advantages = req.Advantages, threats = req.Threats,
                triumphs = req.Triumphs, despairs = req.Despairs,
                difficulty = project.Difficulty, time = project.Time, cost = project.Cost,
                spends = project.Spends.Select(s => new { s.SpendCode, s.Count, s.Parameter, s.PaidWith }),
                createdItemId = project.CreatedCharacterItemId,
            });

        await db.SaveChangesAsync(ct);
        return CraftingMapper.ToDto(project);
    }

    /// <summary>
    /// «Эффект другого зелья» проверяется по каталогу, а не на слово: редкость donor'а должна быть
    /// строго меньше редкости варимого зелья.
    /// </summary>
    private static async Task EnsureCombineTargetsAsync(
        IAppDbContext db, IReadOnlyList<CraftingSpendChoice> choices,
        IReadOnlyDictionary<string, CraftingSpendDef> catalog, ItemDef target, CancellationToken ct)
    {
        foreach (var choice in choices)
        {
            if (!catalog.TryGetValue(choice.Code, out var def)) continue;
            if (def.Effect != CraftingSpendEffect.CombineDose) continue;

            // Клиент присылает id записи каталога: искать по названию нельзя — «Эликсир» может
            // оказаться и зельем, и именем.
            var donor = Guid.TryParse(choice.Parameter.Trim(), out var donorId)
                ? await db.ItemDefs.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == donorId && i.System == target.System, ct)
                : null;
            if (donor is null)
                throw new DomainRuleException(
                    "Второе зелье не найдено в каталоге.", "crafting.combine_unknown");
            if ((donor.Rarity ?? 0) >= (target.Rarity ?? 0))
                throw new DomainRuleException(
                    "Добавить можно только зелье строго меньшей редкости.", "crafting.combine_rarity");
        }
    }
}

/// <summary>Отмена незавершённого проекта. Разрешённый проект остаётся историей.</summary>
public class CancelCraftingHandler(IAppDbContext db) : ICommandHandler<CancelCraftingCommand, Unit>
{
    public async Task<Unit> Handle(CancelCraftingCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var project = await db.CraftingProjects
                .FirstOrDefaultAsync(p => p.Id == command.ProjectId && p.CharacterId == c.Id, ct)
            ?? throw new DomainRuleException("Проект не найден.", "crafting.project_not_found");
        if (project.Status != CraftingProjectStatus.Draft)
            throw new DomainRuleException(
                "Разрешённый проект не отменяется — он уже история.", "crafting.project_not_draft");

        project.Status = CraftingProjectStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Все проекты персонажа, свежие сверху.</summary>
public class GetCraftingProjectsHandler(IAppDbContext db)
    : IQueryHandler<GetCraftingProjectsQuery, List<CraftingProjectDto>>
{
    public async Task<List<CraftingProjectDto>> Handle(GetCraftingProjectsQuery q, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(q.UserId, q.CharacterId, ct: ct);
        var projects = await db.CraftingProjects.AsNoTracking()
            .Include(p => p.Spends)
            .Where(p => p.CharacterId == c.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        return [.. projects.Select(CraftingMapper.ToDto)];
    }
}

/// <summary>Проекции проектов и трат в DTO.</summary>
public static class CraftingMapper
{
    public static CraftingSpendDto ToDto(CraftingSpendDef s) => new(
        s.Code, s.RowCode, s.Table, s.NameRu, s.Name, s.Description, s.DescriptionEn,
        s.AdvantageCost, s.ThreatCost, s.TriumphCost, s.DespairCost,
        s.IsNegative, s.Repeatable, s.RequiresGmConfirmation, s.RequiresParameter,
        s.Effect, s.WeaponOnly, s.SortOrder);

    public static CraftingProjectDto ToDto(CraftingProject p) => new(
        p.Id, p.Kind, p.Status, p.ItemDefId, p.BaseCharacterItemId, p.TargetName, p.TargetPrice, p.TargetRarity,
        p.SkillName, p.BaseDifficulty, p.Difficulty, p.DifficultyReason,
        p.BaseTime, p.Time, CraftingCalc.TimeUnit(p.Kind), p.TimeReason,
        p.ListedCost, p.CostPercent, p.CostOverride, p.CostOverrideReason, p.Cost,
        p.Requirements, p.Intent, p.RoughSurvival,
        p.NetSuccesses, p.Advantages, p.Threats, p.Triumphs, p.Despairs,
        p.CreatedCharacterItemId, p.Outcome,
        [.. p.Spends.Select(s => new CraftingProjectSpendDto(
            s.SpendCode, s.Count, s.Parameter, s.PaidWith, s.TextRu, s.TextEn))],
        p.CreatedAt, p.ResolvedAt);
}
