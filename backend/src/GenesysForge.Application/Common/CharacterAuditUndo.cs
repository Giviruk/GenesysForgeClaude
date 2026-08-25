using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>Безопасная проверка возможности отменить конкретную покупку из истории.</summary>
public static class CharacterAuditUndo
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public sealed record Target(bool IsTalent, Guid DefinitionId, int ExpectedRank);

    private sealed record PurchaseData(
        Guid? SkillDefId,
        Guid? TalentDefId,
        string? Skill,
        string? Talent,
        int Rank,
        Guid? RevertedAuditId);

    public static bool CanUndo(
        CharacterAuditEntry entry,
        Character character,
        IReadOnlyList<CharacterAuditEntry> history) =>
        TryResolve(entry, character, history, out _);

    public static bool TryResolve(
        CharacterAuditEntry entry,
        Character character,
        IReadOnlyList<CharacterAuditEntry> history,
        out Target target)
    {
        target = null!;
        if (entry.Action is not (CharacterAuditAction.SkillRankBought or CharacterAuditAction.TalentBought))
            return false;

        var data = Read(entry);
        if (data is null || data.Rank < 1 || data.RevertedAuditId is not null)
            return false;
        if (history.Any(other => IsReversionOf(other, entry.Id)))
            return false;

        var isTalent = entry.Action == CharacterAuditAction.TalentBought;
        if (!TryResolveDefinitionId(data, isTalent, character, out var definitionId))
            return false;

        // В истории всегда показывается только последняя актуальная покупка этого объекта. Если
        // после неё был повторный ранг, нажатие на старую строку не должно вернуть новый ранг.
        var entryIndex = history.ToList().FindIndex(other => other.Id == entry.Id);
        if (entryIndex < 0 || history.Take(entryIndex).Any(other =>
                other.Action == entry.Action
                && TryResolveDefinitionId(Read(other), isTalent, character, out var otherId)
                && otherId == definitionId))
            return false;

        if (isTalent)
        {
            var row = character.Talents.FirstOrDefault(value => value.TalentDefId == definitionId);
            if (row?.TalentDef is null || row.Ranks != data.Rank)
                return false;

            var result = PurchaseValidator.UndoTalent(
                row.TalentDef.Tier, row.Ranks, TalentTierCounter.Count(character.Talents));
            if (!result.Allowed)
                return false;

            var dependencyError = TalentPurchasePolicy.ValidateRefund(
                row.TalentDef, row.Ranks - 1,
                character.Talents.Where(value => value.TalentDef is not null).Select(value => value.TalentDef!));
            if (dependencyError is not null)
                return false;
        }
        else
        {
            var row = character.Skills.FirstOrDefault(value => value.SkillDefId == definitionId);
            if (row is null || row.Ranks != data.Rank)
                return false;
            if (!PurchaseValidator.UndoSkillRank(row.Ranks, row.FreeRanks, row.IsCareer).Allowed)
                return false;
        }

        target = new Target(isTalent, definitionId, data.Rank);
        return true;
    }

    private static PurchaseData? Read(CharacterAuditEntry? entry)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.DataJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<PurchaseData>(entry.DataJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsReversionOf(CharacterAuditEntry entry, Guid sourceId) =>
        entry.Action is CharacterAuditAction.SkillRankRefunded or CharacterAuditAction.TalentRefunded
        && Read(entry)?.RevertedAuditId == sourceId;

    private static bool TryResolveDefinitionId(
        PurchaseData? data,
        bool isTalent,
        Character character,
        out Guid definitionId)
    {
        definitionId = Guid.Empty;
        if (data is null)
            return false;

        var explicitId = isTalent ? data.TalentDefId : data.SkillDefId;
        if (explicitId is { } id)
        {
            definitionId = id;
            return true;
        }

        var name = isTalent ? data.Talent : data.Skill;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var ids = isTalent
            ? character.Talents.Where(value => value.TalentDef?.Name == name).Select(value => value.TalentDefId).Distinct().ToList()
            : character.Skills.Where(value => value.SkillDef?.Name == name).Select(value => value.SkillDefId).Distinct().ToList();
        if (ids.Count != 1)
            return false;
        definitionId = ids[0];
        return true;
    }
}
