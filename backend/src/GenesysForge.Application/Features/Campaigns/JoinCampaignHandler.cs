using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class JoinCampaignHandler(IAppDbContext db) : ICommandHandler<JoinCampaignCommand, CampaignDetailDto>
{
    public async Task<CampaignDetailDto> Handle(JoinCampaignCommand command, CancellationToken ct = default)
    {
        var code = (command.Request.JoinCode ?? "").Trim().ToUpperInvariant();
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.JoinCode == code, ct)
            ?? throw new DomainRuleException("Кампания с таким кодом не найдена.");

        // игрок может добавить только своего персонажа
        var character = await db.GetOwnedAsync(command.UserId, command.Request.CharacterId, tracking: false, ct);

        var already = await db.CampaignCharacters.AnyAsync(
            cc => cc.CampaignId == campaign.Id && cc.CharacterId == character.Id, ct);
        if (already) throw new DomainRuleException("Этот персонаж уже участвует в кампании.");

        db.CampaignCharacters.Add(new CampaignCharacter
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            CharacterId = character.Id,
            PlayerUserId = command.UserId,
        });
        await db.SaveChangesAsync(ct);
        return await CampaignMapper.BuildDetailAsync(db, campaign, command.UserId, ct);
    }
}
