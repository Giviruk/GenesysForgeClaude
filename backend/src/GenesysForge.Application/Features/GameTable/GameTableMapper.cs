using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
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

    public static async Task<GameSessionDto> ToDtoAsync(
        IAppDbContext db, GameSession s, bool isGm, CancellationToken ct = default)
    {
        var visible = s.Participants.Where(p => isGm || !p.IsHiddenFromPlayers).ToList();
        var characterIds = visible.Where(p => p.CharacterId != null).Select(p => p.CharacterId!.Value).Distinct().ToList();
        var npcIds = visible.Where(p => p.NpcId != null).Select(p => p.NpcId!.Value).Distinct().ToList();
        var characters = await db.Characters.AsNoTracking().Where(c => characterIds.Contains(c.Id))
            .Select(c => new { c.Id, c.PortraitUrl }).ToDictionaryAsync(c => c.Id, c => c.PortraitUrl, ct);
        var npcs = await db.Npcs.AsNoTracking().Where(n => npcIds.Contains(n.Id))
            .Select(n => new { n.Id, n.PortraitUrl }).ToDictionaryAsync(n => n.Id, n => n.PortraitUrl, ct);
        // Игрок не видит скрытых участников, заметок мастера и приватных заметок участников.
        var participants = visible
            .OrderBy(p => p.Order).ThenBy(p => p.DisplayName)
            .Select(p =>
            {
                var group = MinionGroupRules.Calculate(
                    p.ParticipantType, p.Count, p.WoundsCurrent, p.WoundsThreshold, p.IsDefeated);
                return new GameParticipantDto(
                    p.Id, p.CharacterId, p.NpcId, p.DisplayName, p.ParticipantType, p.InitiativeSlotType,
                    p.Count, group?.RemainingCount ?? p.Count, group?.PerMemberWoundThreshold,
                    p.WoundsCurrent, p.WoundsThreshold, p.StrainCurrent, p.StrainThreshold,
                    p.Soak, p.MeleeDefense, p.RangedDefense, p.BoostDice, p.SetbackDice,
                    p.CriticalInjuries, p.IsActive, p.IsDefeated,
                    p.IsHiddenFromPlayers, isGm ? p.Notes : "", p.RangeZone, p.RangeAngle, p.Order,
                    p.CharacterId is { } characterId ? characters.GetValueOrDefault(characterId)
                        : p.NpcId is { } npcId ? npcs.GetValueOrDefault(npcId) : null);
            })
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
