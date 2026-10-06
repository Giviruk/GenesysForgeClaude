using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class BuyTalentHandler(IAppDbContext db) : ICommandHandler<BuyTalentCommand, Unit>
{
    public async Task<Unit> Handle(BuyTalentCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, command.UserId, c.System, command.CharacterId, ct: ct);
        var talentDef = await db.TalentDefs.FirstOrDefaultAsync(t =>
                t.Id == command.TalentDefId && t.System == c.System
                && (t.OwnerUserId == null
                    || (t.HomebrewPackId == null ? t.OwnerUserId == command.UserId
                        : visiblePackIds.Contains(t.HomebrewPackId.Value))), ct)
            ?? throw new DomainRuleException("Талант не найден.");

        var row = c.Talents.FirstOrDefault(t => t.TalentDefId == command.TalentDefId);

        // Структурные ограничения (retired, prerequisite, взаимоисключения) проверяются
        // до пирамиды и XP и до любой мутации: невалидный запрос не меняет ничего.
        var ownedCodes = c.Talents
            .Where(t => t.TalentDef is not null)
            .Select(t => TalentPurchasePolicy.BareCode(t.TalentDef!.Code))
            .Where(code => code.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        // Имена связанных талантов читаются из каталога: предусловие как раз и не куплено,
        // поэтому искать его среди талантов персонажа бессмысленно.
        var relatedNames = await RelatedTalentNamesAsync(talentDef, c.System, ct);
        var policyError = TalentPurchasePolicy.ValidatePurchase(
            talentDef,
            new TalentPurchasePolicy.OwnedTalents(ownedCodes),
            code => relatedNames.GetValueOrDefault(code, code));
        if (policyError is not null)
            throw new DomainRuleException(policyError.Message, policyError.ReasonCode);

        var result = PurchaseValidator.BuyTalent(
            talentDef.Tier,
            row?.Ranks ?? 0,
            talentDef.IsRanked,
            TalentTierCounter.Count(c.Talents),
            c.AvailableXp);
        if (!result.Allowed) throw new DomainRuleException(result.Error!, TalentPurchasePolicy.ReasonPyramidOrXp);

        // Обязательный выбор ранга (ROT-TAL-03) проверяется до списания XP. Старое поле
        // Characteristic принимается как алиас только для Dedication.
        var schema = TalentChoiceSchemas.For(talentDef);
        var rankIndex = row?.Ranks ?? 0;
        var requestedChoices = TalentChoiceSchemas.Normalize(schema,
            command.Choices?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? []);
        if (requestedChoices.Count == 0 && schema.Kind == TalentChoiceKind.Characteristic
            && command.Characteristic is { } legacyChoice)
            requestedChoices = [legacyChoice.ToString()];

        var alreadyChosen = (row?.Choices ?? []).Select(x => x.Value).ToList();
        var skills = schema.Kind == TalentChoiceKind.Skill
            ? await SkillsAsync(c.System, command.UserId, ct)
            : new Dictionary<string, (SkillKind Kind, string NameRu)>(StringComparer.Ordinal);
        var choiceError = TalentChoiceSchemas.Validate(
            schema, rankIndex, requestedChoices, alreadyChosen,
            name => skills.TryGetValue(name, out var skill) ? skill.Kind : null);
        if (choiceError is not null)
            throw new DomainRuleException(choiceError.Message, choiceError.ReasonCode);

        // Signature Spell: действие и эффекты сверяются со справочником магии системы персонажа,
        // снимок имени собирается из русских названий записей.
        var spellNames = schema.Kind == TalentChoiceKind.SpellConfiguration
            ? await SpellConfigurationNamesAsync(c.System, command.UserId, requestedChoices, ct)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        // Animal Companion хранит стабильный id записи NPC, а не имя. Одновременно проверяем,
        // что этот NPC видим игроку, относится к той же системе, помечен как животное и
        // помещается в лимит силуэта текущего ранга (первый ранг — 0, каждый следующий +1).
        var companionNames = schema.Kind == TalentChoiceKind.AnimalCompanion
            ? await CompanionNamesAsync(c, command.UserId, rankIndex, requestedChoices, ct)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        // Dedication дополнительно ограничен потолком характеристики.
        CharacteristicType? grant = null;
        if (talentDef.GrantsCharacteristic)
        {
            var chosen = Enum.Parse<CharacteristicType>(requestedChoices[0], ignoreCase: true);
            if (c.GetCharacteristic(chosen) >= GenesysRules.MaxCharacteristicAtCreation)
                throw new DomainRuleException(
                    $"Талант не может увеличить характеристику выше {GenesysRules.MaxCharacteristicAtCreation}.",
                    "talent.choice.characteristic_capped");
            grant = chosen;
        }

        if (row is null)
        {
            row = new CharacterTalent
            {
                Id = Guid.NewGuid(), CharacterId = c.Id, TalentDefId = command.TalentDefId,
                TalentDef = talentDef, Ranks = 0,
            };
            db.CharacterTalents.Add(row);
            c.Talents.Add(row);
        }
        row.Ranks++;
        row.NeedsChoice = false;
        foreach (var value in requestedChoices)
        {
            var choice = new CharacterTalentChoice
            {
                Id = Guid.NewGuid(),
                CharacterTalentId = row.Id,
                RankIndex = rankIndex,
                Kind = schema.Kind,
                Value = value,
                DisplayName = schema.Kind switch
                {
                    TalentChoiceKind.AnimalCompanion => companionNames.GetValueOrDefault(value, value),
                    TalentChoiceKind.SpellConfiguration => spellNames.GetValueOrDefault(value, value),
                    TalentChoiceKind.Skill when skills.TryGetValue(value, out var skill)
                        && skill.NameRu.Length > 0 => skill.NameRu,
                    _ => DisplayNameFor(schema.Kind, value),
                },
            };
            // Выбор следующего ранга добавляется к уже отслеживаемому таланту: без явного Add
            // EF принимает запись с заданным ключом за существующую и пытается её обновить.
            db.CharacterTalentChoices.Add(choice);
            row.Choices.Add(choice);
        }
        if (grant is { } g)
        {
            c.IncreaseCharacteristic(g);
            row.SetGrants([.. row.ParseGrants(), g]);
        }
        c.SpentXp += result.Cost;

        var grantNote = grant is { } gc ? $" (+1 к «{CharacterAudit.CharacteristicLabel(gc)}»)" : "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.TalentBought,
            $"Куплен талант «{talentDef.Name}» (→{row.Ranks}){grantNote}", -result.Cost,
            new
            {
                talentDefId = command.TalentDefId, talent = talentDef.Name, rank = row.Ranks,
                cost = result.Cost, grant = grant?.ToString(),
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    /// <summary>
    /// Вид и русское имя каждого навыка системы по каноническому имени — для валидации выбора
    /// навыков и снимка отображаемого имени.
    /// </summary>
    private async Task<Dictionary<string, (SkillKind Kind, string NameRu)>> SkillsAsync(
        GameSystem system, Guid userId, CancellationToken ct)
    {
        var rows = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .Select(s => new { s.Name, s.NameRu, s.Kind, s.OwnerUserId })
            .ToListAsync(ct);
        return rows
            .OrderBy(s => s.OwnerUserId == null ? 0 : 1)
            .GroupBy(s => s.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (g.First().Kind, g.First().NameRu.Trim()), StringComparer.Ordinal);
    }

    /// <summary>
    /// Проверяет конфигурации Signature Spell по справочнику магии и возвращает их снимки имён:
    /// «Атака: Огонь, Дистанция ×2».
    /// </summary>
    private async Task<Dictionary<string, string>> SpellConfigurationNamesAsync(
        GameSystem system, Guid userId, IReadOnlyList<string> values, CancellationToken ct)
    {
        // Одно и то же действие и его эффекты повторяются по магическим навыкам — для проверки
        // конфигурации навык не важен, берём любую запись с нужным кодом.
        var rows = await db.SpellDefs.AsNoTracking()
            .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .Select(s => new { s.Kind, s.ParentEffect, s.NameEn, s.NameRu, s.Repeatable, s.Exclusions })
            .ToListAsync(ct);
        var actions = rows.Where(s => s.Kind == SpellEntryKind.Effect)
            .GroupBy(s => s.NameEn, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().NameRu, StringComparer.Ordinal);
        var effects = rows.Where(s => s.Kind == SpellEntryKind.AdditionalEffect)
            .GroupBy(s => (s.ParentEffect, s.NameEn))
            .ToDictionary(g => g.Key, g => g.First());

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var configuration = SignatureSpellConfiguration.Parse(value)!;
            var error = SignatureSpellRules.Validate(configuration,
                actions.ContainsKey,
                (action, code) => effects.TryGetValue((action, code), out var e)
                    ? new SignatureSpellEffect(e.NameEn, e.Repeatable,
                        e.Exclusions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    : null);
            if (error is not null)
                throw new DomainRuleException(error.Message, error.ReasonCode);

            var effectNames = configuration.Effects
                .GroupBy(code => code, StringComparer.Ordinal)
                .Select(g =>
                {
                    var name = effects[(configuration.Action, g.Key)].NameRu;
                    return g.Count() > 1 ? $"{name} ×{g.Count()}" : name;
                });
            var display = $"{actions[configuration.Action]}: {string.Join(", ", effectNames)}";
            names[value] = display.Length <= TalentChoiceSchemas.MaxValueLength
                ? display
                : display[..(TalentChoiceSchemas.MaxValueLength - 1)] + "…";
        }
        return names;
    }

    /// <summary>Снимок отображаемого имени выбора; для характеристик — русская метка.</summary>
    private static string DisplayNameFor(TalentChoiceKind kind, string value) =>
        kind == TalentChoiceKind.Characteristic
            && TalentChoiceSchemas.TryParseCharacteristic(value, out var ch)
            ? CharacterAudit.CharacteristicLabel(ch)
            : value;

    private async Task<Dictionary<string, string>> CompanionNamesAsync(
        Character character, Guid userId, int maximumSilhouette, IReadOnlyList<string> values,
        CancellationToken ct)
    {
        var ids = new List<Guid>(values.Count);
        foreach (var value in values)
        {
            if (!Guid.TryParse(value, out var id))
                throw new DomainRuleException(
                    "Выберите спутника из доступной библиотеки NPC.", "talent.choice.companion_unknown");
            ids.Add(id);
        }

        var companions = await db.Npcs.AsNoTracking()
            .Where(n => ids.Contains(n.Id) && !n.Retired && n.System == character.System)
            .Where(n => n.OwnerUserId == userId || n.IsBuiltIn
                || (n.Visibility == NpcVisibility.CampaignVisible && n.CampaignId != null
                    && db.CampaignCharacters.Any(cc => cc.PlayerUserId == userId
                        && cc.CampaignId == n.CampaignId.Value)))
            .Select(n => new { n.Id, n.Name, n.Silhouette, n.Tags })
            .ToListAsync(ct);
        if (companions.Count != ids.Distinct().Count())
            throw new DomainRuleException(
                "Спутник не найден или недоступен персонажу.", "talent.choice.companion_unknown");

        var notAnimal = companions.FirstOrDefault(n => !IsAnimal(n.Tags));
        if (notAnimal is not null)
            throw new DomainRuleException(
                $"«{notAnimal.Name}» не помечен как животное и не может быть выбран спутником.",
                "talent.choice.companion_not_animal");

        var tooLarge = companions.FirstOrDefault(n => n.Silhouette > maximumSilhouette);
        if (tooLarge is not null)
            throw new DomainRuleException(
                $"Силуэт спутника «{tooLarge.Name}» не должен превышать {maximumSilhouette}.",
                "talent.choice.companion_silhouette");

        return values.ToDictionary(
            value => value,
            value => companions.Single(n => n.Id == Guid.Parse(value)).Name,
            StringComparer.Ordinal);
    }

    private static bool IsAnimal(IEnumerable<string> tags) => tags.Any(tag =>
        tag.Trim().Equals("animal", StringComparison.OrdinalIgnoreCase)
        || tag.Trim().Equals("животное", StringComparison.OrdinalIgnoreCase)
        || tag.Trim().Equals("зверь", StringComparison.OrdinalIgnoreCase));

    /// <summary>Имена предусловия и взаимоисключений таланта по их bare-slug кодам.</summary>
    private async Task<Dictionary<string, string>> RelatedTalentNamesAsync(
        TalentDef definition, GameSystem system, CancellationToken ct)
    {
        var codes = definition.ExcludesTalentCodes
            .Append(definition.RequiresTalentCode)
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (codes.Count == 0) return [];

        var prefix = system == GameSystem.GenesysCore ? "gc.talent." : "rot.talent.";
        var fullCodes = codes.Select(code => prefix + code).ToList();
        var rows = await db.TalentDefs.AsNoTracking()
            .Where(t => t.OwnerUserId == null && fullCodes.Contains(t.Code))
            .Select(t => new { t.Code, t.Name })
            .ToListAsync(ct);

        return rows.ToDictionary(
            r => TalentPurchasePolicy.BareCode(r.Code), r => r.Name, StringComparer.Ordinal);
    }
}
