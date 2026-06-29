using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.GameTable;

public record UpdateParticipantCommand(
    Guid UserId, Guid CampaignId, Guid ParticipantId, UpdateParticipantRequest Request) : ICommand<GameSessionDto>;

public class UpdateParticipantHandler(IAppDbContext db) : ICommandHandler<UpdateParticipantCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(UpdateParticipantCommand command, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        var isGm = campaign.GmUserId == command.UserId;
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var p = session.Participants.FirstOrDefault(x => x.Id == command.ParticipantId)
            ?? throw new DomainRuleException("Участник не найден.");
        var r = command.Request;

        if (!isGm)
        {
            // Игрок может менять только wounds/strain своего персонажа и только если разрешено.
            if (!session.AllowPlayerEdits)
                throw new DomainRuleException("Мастер не разрешил игрокам менять состояние.");
            var ownsCharacter = p.CharacterId is { } cid && await db.Characters.AsNoTracking()
                .AnyAsync(c => c.Id == cid && c.OwnerUserId == command.UserId, ct);
            if (!ownsCharacter)
                throw new DomainRuleException("Можно менять только своего персонажа.");
            ApplyVitals(p, r);
        }
        else
        {
            ApplyVitals(p, r);
            if (r.DisplayName is not null && !string.IsNullOrWhiteSpace(r.DisplayName)) p.DisplayName = r.DisplayName.Trim();
            if (r.WoundsThreshold is { } wt) p.WoundsThreshold = Math.Max(1, wt);
            if (r.StrainThreshold is { } st) p.StrainThreshold = Math.Max(0, st);
            if (r.Soak is { } soak) p.Soak = Math.Max(0, soak);
            if (r.MeleeDefense is { } md) p.MeleeDefense = Math.Max(0, md);
            if (r.RangedDefense is { } rd) p.RangedDefense = Math.Max(0, rd);
            if (r.IsActive is { } active) p.IsActive = active;
            if (r.IsDefeated is { } defeated) p.IsDefeated = defeated;
            if (r.IsHiddenFromPlayers is { } hidden) p.IsHiddenFromPlayers = hidden;
            if (r.Notes is not null) p.Notes = r.Notes;
            if (r.InitiativeSlotType is { } slot) p.InitiativeSlotType = slot;
        }
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return GameTableMapper.ToDto(session, isGm);
    }

    private static void ApplyVitals(GameParticipant p, UpdateParticipantRequest r)
    {
        if (r.WoundsCurrent is { } w) p.WoundsCurrent = Math.Max(0, w);
        if (r.StrainCurrent is { } s) p.StrainCurrent = Math.Max(0, s);
        if (r.CriticalInjuries is { } ci) p.CriticalInjuries = Math.Max(0, ci);
    }
}
