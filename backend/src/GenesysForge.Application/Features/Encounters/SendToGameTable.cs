using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.GameTable;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Encounters;

/// <summary>Отправка подготовленного энкаунтера в Game Table (§6.6).</summary>
public record SendToGameTableCommand(Guid UserId, Guid EncounterId, SendToTableMode Mode) : ICommand<GameSessionDto>;

public class SendToGameTableHandler(IAppDbContext db) : ICommandHandler<SendToGameTableCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(SendToGameTableCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.EncounterId, ct);

        var active = await GameTableMapper.LoadActiveAsync(db, encounter.CampaignId, ct, tracking: true);
        GameSession session;

        if (active is null)
        {
            session = CreateSessionFromEncounter(encounter);
            db.GameSessions.Add(session);
        }
        else if (command.Mode == SendToTableMode.Append)
        {
            session = active;
        }
        else // Replace: завершаем текущую сцену и создаём новую
        {
            active.IsActive = false;
            active.UpdatedAt = DateTime.UtcNow;
            session = CreateSessionFromEncounter(encounter);
            db.GameSessions.Add(session);
        }

        var order = session.Participants.Count == 0 ? 0 : session.Participants.Max(p => p.Order) + 1;
        foreach (var ep in encounter.Participants.OrderBy(p => p.Order))
        {
            var participant = await ParticipantFactory.CreateAsync(
                db, session.Id, encounter.CampaignId, ToAddRequest(ep), ct);
            ApplyEncounterState(participant, ep);
            participant.Order = order++;
            session.Participants.Add(participant);
        }

        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var fresh = await GameTableMapper.RequireActiveAsync(db, encounter.CampaignId, ct);
        return GameTableMapper.ToDto(fresh, isGm: true);
    }

    private static GameSession CreateSessionFromEncounter(Encounter e) => new()
    {
        Id = Guid.NewGuid(),
        CampaignId = e.CampaignId,
        Name = e.Name,
        Description = e.PlayerDescription,
        PublicNotes = e.PlayerDescription,
        // приватные заметки мастера: описание для GM, цели NPC и осложнения
        GmNotes = string.Join("\n\n", new[] { e.GmDescription, e.NpcGoals, e.Complications }
            .Where(s => !string.IsNullOrWhiteSpace(s))),
    };

    private static AddParticipantRequest ToAddRequest(EncounterParticipant ep) => new(
        CharacterId: ep.CharacterId,
        NpcId: ep.NpcId,
        DisplayName: ep.DisplayName,
        ParticipantType: ep.ParticipantType,
        InitiativeSlotType: ep.InitiativeSide,
        Count: ep.Quantity,
        WoundsThreshold: ep.StartingWoundsOverride,
        StrainThreshold: ep.StartingStrainOverride,
        Soak: null,
        MeleeDefense: null,
        RangedDefense: null);

    private static void ApplyEncounterState(GameParticipant p, EncounterParticipant ep)
    {
        p.IsHiddenFromPlayers = ep.StartsHidden;
        p.IsDefeated = ep.StartsDefeated;
        if (!string.IsNullOrWhiteSpace(ep.Notes)) p.Notes = ep.Notes;
        if (ep.StartingWoundsOverride is { } wt and > 0) p.WoundsThreshold = wt;
        if (ep.StartingStrainOverride is { } st and > 0) p.StrainThreshold = st;
    }
}
