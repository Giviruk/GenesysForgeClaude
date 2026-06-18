using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Encounters;

/// <summary>Сборка участника энкаунтера из персонажа кампании, NPC библиотеки или вручную.</summary>
public static class EncounterParticipantFactory
{
    public static async Task<EncounterParticipant> CreateAsync(
        IAppDbContext db, Guid encounterId, Guid campaignId, AddEncounterParticipantRequest req, CancellationToken ct)
    {
        if (req.CharacterId is { } characterId)
            return await FromCharacterAsync(db, encounterId, campaignId, characterId, req, ct);
        if (req.NpcId is { } npcId)
            return await FromNpcAsync(db, encounterId, npcId, req, ct);
        return FromManual(encounterId, req);
    }

    private static async Task<EncounterParticipant> FromCharacterAsync(
        IAppDbContext db, Guid encounterId, Guid campaignId, Guid characterId,
        AddEncounterParticipantRequest req, CancellationToken ct)
    {
        var c = await db.CampaignCharacters.AsNoTracking()
            .Where(cc => cc.CampaignId == campaignId && cc.CharacterId == characterId)
            .Select(cc => cc.Character!)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainRuleException("Персонаж не состоит в этой кампании.");

        return new EncounterParticipant
        {
            Id = Guid.NewGuid(),
            EncounterId = encounterId,
            CharacterId = c.Id,
            DisplayName = c.Name,
            ParticipantType = ParticipantType.PlayerCharacter,
            InitiativeSide = req.InitiativeSide ?? InitiativeSlotType.Player,
            Quantity = 1,
            Notes = req.Notes?.Trim() ?? "",
            StartsHidden = req.StartsHidden ?? false,
            StartsDefeated = req.StartsDefeated ?? false,
            StartingWoundsOverride = req.StartingWoundsOverride,
            StartingStrainOverride = req.StartingStrainOverride,
        };
    }

    private static async Task<EncounterParticipant> FromNpcAsync(
        IAppDbContext db, Guid encounterId, Guid npcId, AddEncounterParticipantRequest req, CancellationToken ct)
    {
        var npc = await db.Npcs.AsNoTracking().FirstOrDefaultAsync(n => n.Id == npcId, ct)
            ?? throw new DomainRuleException("NPC не найден.");

        var quantity = Math.Max(1, req.Quantity ?? 1);
        var type = req.ParticipantType
            ?? (quantity > 1 ? ParticipantType.MinionGroup : ParticipantType.Npc);

        return new EncounterParticipant
        {
            Id = Guid.NewGuid(),
            EncounterId = encounterId,
            NpcId = npc.Id,
            DisplayName = npc.Name,
            ParticipantType = type,
            InitiativeSide = req.InitiativeSide ?? InitiativeSlotType.Npc,
            Quantity = quantity,
            Notes = req.Notes?.Trim() ?? "",
            StartsHidden = req.StartsHidden ?? false,
            StartsDefeated = req.StartsDefeated ?? false,
            StartingWoundsOverride = req.StartingWoundsOverride,
            StartingStrainOverride = req.StartingStrainOverride,
        };
    }

    private static EncounterParticipant FromManual(Guid encounterId, AddEncounterParticipantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            throw new DomainRuleException("Укажите имя участника.");

        return new EncounterParticipant
        {
            Id = Guid.NewGuid(),
            EncounterId = encounterId,
            DisplayName = req.DisplayName!.Trim(),
            ParticipantType = req.ParticipantType ?? ParticipantType.Hazard,
            InitiativeSide = req.InitiativeSide ?? InitiativeSlotType.Npc,
            Quantity = Math.Max(1, req.Quantity ?? 1),
            Notes = req.Notes?.Trim() ?? "",
            StartsHidden = req.StartsHidden ?? false,
            StartsDefeated = req.StartsDefeated ?? false,
            StartingWoundsOverride = req.StartingWoundsOverride,
            StartingStrainOverride = req.StartingStrainOverride,
        };
    }
}
