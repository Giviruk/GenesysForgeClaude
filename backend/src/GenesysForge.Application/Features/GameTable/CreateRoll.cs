using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.GameTable;

/// <summary>Записать бросок (посчитанный клиентом) в лог стола кампании.</summary>
public record CreateRollCommand(Guid UserId, Guid CampaignId, CreateRollRequest Request)
    : ICommand<RollLogEntryDto>;

public class CreateRollHandler(IAppDbContext db) : ICommandHandler<CreateRollCommand, RollLogEntryDto>
{
    public async Task<RollLogEntryDto> Handle(CreateRollCommand command, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        var isGm = campaign.GmUserId == command.UserId;

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.PoolJson) || string.IsNullOrWhiteSpace(req.ResultJson))
            throw new DomainRuleException("Пустой бросок.");

        // Имя берём из запроса, иначе — отображаемое имя пользователя.
        var actorName = req.ActorName?.Trim();
        if (string.IsNullOrEmpty(actorName))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
                ?? throw new DomainRuleException("Пользователь не найден.");
            actorName = user.DisplayName;
        }

        var session = await GameTableMapper.LoadActiveAsync(db, campaign.Id, ct);

        var entry = new RollLogEntry
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            SessionId = session?.Id,
            ActorUserId = command.UserId,
            ActorName = actorName,
            Label = req.Label?.Trim() ?? "",
            PoolJson = req.PoolJson,
            ResultJson = req.ResultJson,
            Summary = req.Summary?.Trim() ?? "",
            // Секретный бросок может делать только мастер; игроку флаг игнорируем.
            IsSecret = isGm && req.IsSecret,
        };
        db.RollLogEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return new RollLogEntryDto(
            entry.Id, entry.CampaignId, entry.SessionId, entry.ActorName, entry.Label,
            entry.PoolJson, entry.ResultJson, entry.Summary, entry.IsSecret, entry.CreatedAt);
    }
}
