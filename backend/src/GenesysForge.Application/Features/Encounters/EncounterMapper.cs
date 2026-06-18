using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Encounters;

/// <summary>Загрузка энкаунтеров, проверки доступа и сборка DTO с учётом роли (см. спецификацию §7–8).</summary>
public static class EncounterMapper
{
    public static async Task<Encounter> LoadAsync(
        IAppDbContext db, Guid id, CancellationToken ct, bool tracking = false)
    {
        var query = db.Encounters.Include(e => e.Participants).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new DomainRuleException("Энкаунтер не найден.");
    }

    /// <summary>Энкаунтер кампании, которым владеет GM запроса; иначе ошибка.</summary>
    public static async Task<(Encounter Encounter, Campaign Campaign)> GetAsGmAsync(
        IAppDbContext db, Guid userId, Guid id, CancellationToken ct, bool tracking = false)
    {
        var encounter = await LoadAsync(db, id, ct, tracking);
        var campaign = await CampaignMapper.GetAsGmAsync(db, userId, encounter.CampaignId, ct);
        return (encounter, campaign);
    }

    /// <summary>Энкаунтер, который пользователь имеет право видеть (GM всегда; игрок — если visible).</summary>
    public static async Task<(Encounter Encounter, bool IsGm)> GetViewableAsync(
        IAppDbContext db, Guid userId, Guid id, CancellationToken ct)
    {
        var encounter = await LoadAsync(db, id, ct);
        var campaign = await CampaignMapper.GetAccessibleAsync(db, userId, encounter.CampaignId, ct);
        var isGm = campaign.GmUserId == userId;
        if (!isGm && !encounter.IsVisibleToPlayers)
            throw new DomainRuleException("Энкаунтер не найден.");
        return (encounter, isGm);
    }

    public static EncounterListItemDto ToListItem(Encounter e) => new(
        e.Id, e.Name, e.System, e.Type, e.ThreatLevel, e.IsVisibleToPlayers,
        e.Participants.Count, e.Tags, e.CreatedAt, e.UpdatedAt);

    public static EncounterDetailDto ToDetail(Encounter e, bool isGm)
    {
        // Игрок видит только публичных участников и публичные поля (§7).
        var participants = e.Participants
            .Where(p => isGm || !p.StartsHidden)
            .OrderBy(p => p.Order).ThenBy(p => p.DisplayName)
            .Select(p => new EncounterParticipantDto(
                p.Id, p.CharacterId, p.NpcId, p.DisplayName, p.ParticipantType, p.InitiativeSide,
                p.Quantity, isGm ? p.Notes : "", p.StartsHidden, p.StartsDefeated,
                p.StartingWoundsOverride, p.StartingStrainOverride, p.Order))
            .ToList();

        return new EncounterDetailDto(
            e.Id, e.CampaignId, e.Name, e.System, e.Type, e.ThreatLevel, isGm, e.IsVisibleToPlayers,
            isGm ? e.GmDescription : null,
            e.PlayerDescription,
            e.PlayerGoals,
            isGm ? e.NpcGoals : null,
            e.Location,
            e.Environment,
            isGm ? e.Complications : null,
            e.Rewards,
            e.Tags, participants, e.CreatedAt, e.UpdatedAt);
    }

    public static void Apply(Encounter e, EncounterInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new DomainRuleException("Название энкаунтера не может быть пустым.");

        e.Name = input.Name.Trim();
        e.System = input.System;
        e.Type = input.Type;
        e.ThreatLevel = input.ThreatLevel;
        e.GmDescription = input.GmDescription?.Trim() ?? "";
        e.PlayerDescription = input.PlayerDescription?.Trim() ?? "";
        e.PlayerGoals = input.PlayerGoals?.Trim() ?? "";
        e.NpcGoals = input.NpcGoals?.Trim() ?? "";
        e.Location = input.Location?.Trim() ?? "";
        e.Environment = input.Environment?.Trim() ?? "";
        e.Complications = input.Complications?.Trim() ?? "";
        e.Rewards = input.Rewards?.Trim() ?? "";
        e.IsVisibleToPlayers = input.IsVisibleToPlayers;
        e.Tags = (input.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        e.UpdatedAt = DateTime.UtcNow;
    }
}
