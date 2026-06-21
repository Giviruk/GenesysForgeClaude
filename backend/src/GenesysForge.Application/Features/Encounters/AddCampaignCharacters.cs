using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Encounters;

/// <summary>Добавить персонажей кампании в энкаунтер: всех активных или выбранных (§6.3).</summary>
public record AddCampaignCharactersCommand(Guid UserId, Guid EncounterId, AddCampaignCharactersRequest Request)
    : ICommand<EncounterDetailDto>;

public class AddCampaignCharactersHandler(IAppDbContext db)
    : ICommandHandler<AddCampaignCharactersCommand, EncounterDetailDto>
{
    public async Task<EncounterDetailDto> Handle(AddCampaignCharactersCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.EncounterId, ct, tracking: true);

        var memberQuery = db.CampaignCharacters.AsNoTracking()
            .Where(cc => cc.CampaignId == encounter.CampaignId);
        var requested = command.Request.CharacterIds;
        if (requested is { Count: > 0 })
            memberQuery = memberQuery.Where(cc => requested.Contains(cc.CharacterId));

        var members = await memberQuery
            .Select(cc => new { cc.CharacterId, Name = cc.Character!.Name })
            .ToListAsync(ct);
        if (members.Count == 0)
            throw new DomainRuleException("Не найдено персонажей кампании для добавления.");

        // не дублируем уже добавленных PC
        var existing = encounter.Participants.Where(p => p.CharacterId != null).Select(p => p.CharacterId).ToHashSet();
        var order = encounter.Participants.Count == 0 ? 0 : encounter.Participants.Max(p => p.Order) + 1;

        // Добавляем через DbSet, а не в Include-коллекцию: иначе InMemory-провайдер кидает
        // DbUpdateConcurrencyException. Для ответа перечитываем энкаунтер свежим.
        foreach (var m in members.Where(m => !existing.Contains(m.CharacterId)))
        {
            db.EncounterParticipants.Add(new EncounterParticipant
            {
                Id = Guid.NewGuid(),
                EncounterId = encounter.Id,
                CharacterId = m.CharacterId,
                DisplayName = m.Name,
                ParticipantType = ParticipantType.PlayerCharacter,
                InitiativeSide = InitiativeSlotType.Player,
                Quantity = 1,
                Order = order++,
            });
        }

        encounter.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var fresh = await EncounterMapper.LoadAsync(db, encounter.Id, ct);
        return EncounterMapper.ToDetail(fresh, isGm: true);
    }
}
