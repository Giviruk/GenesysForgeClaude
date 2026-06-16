using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Сборка DTO сцены с учётом роли и загрузка/доступ к сессии.</summary>
public static class GameTableMapper
{
    /// <summary>Активная сессия кампании со связями или null.</summary>
    public static async Task<GameSession?> LoadActiveAsync(
        IAppDbContext db, Guid campaignId, CancellationToken ct, bool tracking = false)
    {
        var query = db.GameSessions
            .Include(s => s.Participants)
            .Include(s => s.Slots)
            .Where(s => s.CampaignId == campaignId && s.IsActive);
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(ct);
    }

    public static async Task<GameSession> RequireActiveAsync(
        IAppDbContext db, Guid campaignId, CancellationToken ct, bool tracking = false) =>
        await LoadActiveAsync(db, campaignId, ct, tracking)
        ?? throw new DomainRuleException("Активная сцена не найдена.");

    public static GameSessionDto ToDto(GameSession s, bool isGm)
    {
        // Игрок не видит скрытых участников, заметок мастера и приватных заметок участников.
        var participants = s.Participants
            .Where(p => isGm || !p.IsHiddenFromPlayers)
            .OrderBy(p => p.Order).ThenBy(p => p.DisplayName)
            .Select(p => new GameParticipantDto(
                p.Id, p.CharacterId, p.NpcId, p.DisplayName, p.ParticipantType, p.InitiativeSlotType,
                p.Count, p.WoundsCurrent, p.WoundsThreshold, p.StrainCurrent, p.StrainThreshold,
                p.Soak, p.MeleeDefense, p.RangedDefense, p.IsActive, p.IsDefeated, p.IsHiddenFromPlayers,
                isGm ? p.Notes : "", p.Order))
            .ToList();

        var slots = s.Slots
            .OrderBy(sl => sl.Order)
            .Select(sl => new InitiativeSlotDto(sl.Id, sl.SlotType, sl.Order, sl.AssignedParticipantId, sl.Notes))
            .ToList();

        return new GameSessionDto(
            s.Id, s.CampaignId, s.Name, s.Description, s.IsActive, isGm, s.AllowPlayerEdits,
            s.PlayerStoryPoints, s.GmStoryPoints, s.CurrentRound, s.CurrentTurnIndex,
            s.PublicNotes, isGm ? s.GmNotes : null, participants, slots);
    }
}
