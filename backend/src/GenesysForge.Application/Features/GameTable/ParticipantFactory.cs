using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Сборка участника сцены из персонажа, NPC или вручную, с копированием базовых параметров.</summary>
public static class ParticipantFactory
{
    public static async Task<GameParticipant> CreateAsync(
        IAppDbContext db, Guid sessionId, Guid campaignId, AddParticipantRequest req, CancellationToken ct)
    {
        if (req.CharacterId is { } characterId)
            return await FromCharacterAsync(db, sessionId, campaignId, characterId, ct);
        if (req.NpcId is { } npcId)
            return await FromNpcAsync(db, sessionId, npcId, req, ct);
        return FromManual(sessionId, req);
    }

    private static async Task<GameParticipant> FromCharacterAsync(
        IAppDbContext db, Guid sessionId, Guid campaignId, Guid characterId, CancellationToken ct)
    {
        // персонаж должен состоять в этой кампании
        var member = await db.CampaignCharacters.AsNoTracking()
            .AnyAsync(cc => cc.CampaignId == campaignId && cc.CharacterId == characterId, ct);
        if (!member) throw new DomainRuleException("Персонаж не состоит в этой кампании.");

        var c = await LoadCharacterAsync(db, characterId, ct);
        var derived = SheetBuilderDerived(c);

        return new GameParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CharacterId = c.Id,
            DisplayName = c.Name,
            ParticipantType = ParticipantType.PlayerCharacter,
            InitiativeSlotType = InitiativeSlotType.Player,
            WoundsCurrent = c.WoundsCurrent,
            WoundsThreshold = derived.WoundThreshold,
            StrainCurrent = c.StrainCurrent,
            StrainThreshold = derived.StrainThreshold,
            Soak = derived.Soak,
            MeleeDefense = derived.MeleeDefense,
            RangedDefense = derived.RangedDefense,
        };
    }

    private static async Task<GameParticipant> FromNpcAsync(
        IAppDbContext db, Guid sessionId, Guid npcId, AddParticipantRequest req, CancellationToken ct)
    {
        var npc = await db.Npcs.AsNoTracking().FirstOrDefaultAsync(n => n.Id == npcId, ct)
            ?? throw new DomainRuleException("NPC не найден.");

        var count = Math.Max(1, req.Count ?? 1);
        var type = req.ParticipantType
            ?? (count > 1 ? ParticipantType.MinionGroup : ParticipantType.Npc);

        // Группа миньонов: общий пул ран = порог × количество (упрощённая модель Genesys).
        var woundThreshold = type == ParticipantType.MinionGroup ? npc.WoundThreshold * count : npc.WoundThreshold;
        var name = count > 1 ? $"{npc.Name} ×{count}" : npc.Name;

        return new GameParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            NpcId = npc.Id,
            DisplayName = name,
            ParticipantType = type,
            InitiativeSlotType = InitiativeSlotType.Npc,
            Count = count,
            WoundsThreshold = woundThreshold,
            WoundsCurrent = 0,
            StrainThreshold = type == ParticipantType.MinionGroup ? null : npc.StrainThreshold,
            Soak = npc.Soak,
            MeleeDefense = npc.MeleeDefense,
            RangedDefense = npc.RangedDefense,
        };
    }

    private static GameParticipant FromManual(Guid sessionId, AddParticipantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            throw new DomainRuleException("Укажите имя участника.");
        var type = req.ParticipantType ?? ParticipantType.Npc;
        return new GameParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            DisplayName = req.DisplayName!.Trim(),
            ParticipantType = type,
            InitiativeSlotType = req.InitiativeSlotType ?? InitiativeSlotType.Npc,
            Count = Math.Max(1, req.Count ?? 1),
            WoundsThreshold = Math.Max(1, req.WoundsThreshold ?? 10),
            WoundsCurrent = 0,
            StrainThreshold = req.StrainThreshold,
            Soak = Math.Max(0, req.Soak ?? 0),
            MeleeDefense = Math.Max(0, req.MeleeDefense ?? 0),
            RangedDefense = Math.Max(0, req.RangedDefense ?? 0),
        };
    }

    private static async Task<Character> LoadCharacterAsync(IAppDbContext db, Guid characterId, CancellationToken ct) =>
        await db.Characters
            .Include(c => c.Archetype)
            .Include(c => c.Career)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, ct)
            ?? throw new DomainRuleException("Персонаж не найден.");

    private static DerivedStats SheetBuilderDerived(Character c)
    {
        var talentInputs = c.Talents.Select(t => new TalentInput(
            t.TalentDef!.Name, t.TalentDef.Tier, t.Ranks,
            t.TalentDef.WoundBonus, t.TalentDef.StrainBonus, t.TalentDef.SoakBonus,
            t.TalentDef.MeleeDefenseBonus, t.TalentDef.RangedDefenseBonus)).ToList();
        var itemInputs = c.Items.Select(i => new ItemInput(
            i.ItemDef!.Name, i.ItemDef.Kind, i.State, i.ItemDef.Encumbrance, i.Quantity,
            i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense, i.ItemDef.RangedDefense,
            i.ItemDef.EncumbranceThresholdBonus)).ToList();
        return SheetCalculator.ComputeDerived(
            c.Characteristics, c.Archetype!.WoundBase, c.Archetype.StrainBase, talentInputs, itemInputs);
    }
}
