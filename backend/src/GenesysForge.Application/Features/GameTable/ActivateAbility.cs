using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Тело активации способности у участника стола.</summary>
public record ActivateAbilityRequest(string AbilityCode);

/// <summary>Результат активации: обновлённая сцена + что применилось и что осталось вручную.</summary>
public record ActivateAbilityResult(
    GameSessionDto Session, string AbilityName, IReadOnlyList<string> Applied, IReadOnlyList<string> Manual);

public record ActivateAbilityCommand(Guid UserId, Guid CampaignId, Guid ParticipantId, ActivateAbilityRequest Request)
    : ICommand<ActivateAbilityResult>;

/// <summary>
/// Активирует героическую способность у участника Game Table (U-18): применяет структурные эффекты
/// (heal/soak/защита/порог) к живому состоянию участника, остальное возвращает как ручную подсказку,
/// и пишет событие в лог стола.
/// </summary>
public class ActivateAbilityHandler(IAppDbContext db) : ICommandHandler<ActivateAbilityCommand, ActivateAbilityResult>
{
    public async Task<ActivateAbilityResult> Handle(ActivateAbilityCommand command, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        var isGm = campaign.GmUserId == command.UserId;
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);

        var p = session.Participants.FirstOrDefault(x => x.Id == command.ParticipantId)
            ?? throw new DomainRuleException("Участник не найден.");

        // Игрок активирует только своего персонажа и только если мастер разрешил правки.
        if (!isGm)
        {
            if (!session.AllowPlayerEdits)
                throw new DomainRuleException("Мастер не разрешил игрокам менять состояние.");
            var ownsCharacter = p.CharacterId is { } cid && await db.Characters.AsNoTracking()
                .AnyAsync(c => c.Id == cid && c.OwnerUserId == command.UserId, ct);
            if (!ownsCharacter)
                throw new DomainRuleException("Можно активировать только у своего персонажа.");
        }

        var ability = await db.HeroicAbilityDefs.Include(h => h.Effects)
            .FirstOrDefaultAsync(h => h.Code == command.Request.AbilityCode, ct)
            ?? throw new DomainRuleException("Способность не найдена.");

        var result = RuleEffectApplier.Apply(ability.Effects, p);
        var name = string.IsNullOrWhiteSpace(ability.NameRu) ? ability.Name : ability.NameRu;
        session.UpdatedAt = DateTime.UtcNow;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        var summary = string.Join("; ", result.Applied.Concat(result.Manual));
        db.RollLogEntries.Add(new RollLogEntry
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            SessionId = session.Id,
            ActorUserId = command.UserId,
            ActorName = user?.DisplayName ?? "",
            Label = $"Способность: {name} — {p.DisplayName}",
            PoolJson = "{}",
            ResultJson = "{}",
            Summary = Trim(summary, 400),
            IsSecret = false,
        });

        await db.SaveChangesAsync(ct);
        return new ActivateAbilityResult(GameTableMapper.ToDto(session, isGm), name, result.Applied, result.Manual);
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
