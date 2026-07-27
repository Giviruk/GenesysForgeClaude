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
                    || (t.OwnerUserId == command.UserId
                        && (t.HomebrewPackId == null || visiblePackIds.Contains(t.HomebrewPackId.Value)))), ct)
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
        var requestedChoices = command.Choices?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? [];
        if (requestedChoices.Count == 0 && schema.Kind == TalentChoiceKind.Characteristic
            && command.Characteristic is { } legacyChoice)
            requestedChoices = [legacyChoice.ToString()];

        var alreadyChosen = (row?.Choices ?? []).Select(x => x.Value).ToList();
        var skillKinds = await SkillKindsAsync(c.System, command.UserId, ct);
        var choiceError = TalentChoiceSchemas.Validate(
            schema, rankIndex, requestedChoices, alreadyChosen,
            name => skillKinds.TryGetValue(name, out var k) ? k : null);
        if (choiceError is not null)
            throw new DomainRuleException(choiceError.Message, choiceError.ReasonCode);

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
            row.Choices.Add(new CharacterTalentChoice
            {
                Id = Guid.NewGuid(),
                CharacterTalentId = row.Id,
                RankIndex = rankIndex,
                Kind = schema.Kind,
                Value = value,
                DisplayName = DisplayNameFor(schema.Kind, value),
            });
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
            new { talent = talentDef.Name, rank = row.Ranks, cost = result.Cost, grant = grant?.ToString() });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    /// <summary>Вид каждого навыка системы по каноническому имени — для валидации выбора навыков.</summary>
    private async Task<Dictionary<string, SkillKind>> SkillKindsAsync(
        GameSystem system, Guid userId, CancellationToken ct)
    {
        var rows = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .Select(s => new { s.Name, s.Kind, s.OwnerUserId })
            .ToListAsync(ct);
        return rows
            .OrderBy(s => s.OwnerUserId == null ? 0 : 1)
            .GroupBy(s => s.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Kind, StringComparer.Ordinal);
    }

    /// <summary>Снимок отображаемого имени выбора; для характеристик — русская метка.</summary>
    private static string DisplayNameFor(TalentChoiceKind kind, string value) =>
        kind == TalentChoiceKind.Characteristic
            && Enum.TryParse<CharacteristicType>(value, ignoreCase: true, out var ch)
            ? CharacterAudit.CharacteristicLabel(ch)
            : value;

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
