using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.GameTable;

public record CreateSessionCommand(Guid UserId, Guid CampaignId, CreateSessionRequest Request) : ICommand<GameSessionDto>;

public class CreateSessionHandler(IAppDbContext db) : ICommandHandler<CreateSessionCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(CreateSessionCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);

        var existing = await GameTableMapper.LoadActiveAsync(db, command.CampaignId, ct);
        if (existing is not null)
            throw new DomainRuleException("В кампании уже есть активная сцена. Завершите её перед созданием новой.");

        if (string.IsNullOrWhiteSpace(command.Request.Name))
            throw new DomainRuleException("Название сцены не может быть пустым.");

        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CampaignId = command.CampaignId,
            Name = command.Request.Name.Trim(),
            Description = command.Request.Description?.Trim() ?? "",
            PlayerStoryPoints = Math.Max(0, command.Request.PlayerStoryPoints),
            GmStoryPoints = Math.Max(0, command.Request.GmStoryPoints),
        };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return GameTableMapper.ToDto(session, isGm: true);
    }
}
