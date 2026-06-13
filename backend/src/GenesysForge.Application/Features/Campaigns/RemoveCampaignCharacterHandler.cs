using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class RemoveCampaignCharacterHandler(IAppDbContext db) : ICommandHandler<RemoveCampaignCharacterCommand, Unit>
{
    public async Task<Unit> Handle(RemoveCampaignCharacterCommand command, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == command.CampaignId, ct)
            ?? throw new DomainRuleException("Кампания не найдена.");
        var link = await db.CampaignCharacters.FirstOrDefaultAsync(
                cc => cc.CampaignId == command.CampaignId && cc.CharacterId == command.CharacterId, ct)
            ?? throw new DomainRuleException("Персонаж не участвует в кампании.");

        // убрать может GM кампании или владелец персонажа
        if (campaign.GmUserId != command.UserId && link.PlayerUserId != command.UserId)
            throw new DomainRuleException("Недостаточно прав, чтобы убрать персонажа из кампании.");

        db.CampaignCharacters.Remove(link);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
