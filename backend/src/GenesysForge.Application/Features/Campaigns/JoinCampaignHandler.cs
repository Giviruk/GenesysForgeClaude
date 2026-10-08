using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class JoinCampaignHandler(IAppDbContext db) : ICommandHandler<JoinCampaignCommand, CampaignDetailDto>
{
    public async Task<CampaignDetailDto> Handle(JoinCampaignCommand command, CancellationToken ct = default)
    {
        var code = (command.Request.JoinCode ?? "").Trim().ToUpperInvariant();
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.JoinCode == code, ct)
            ?? throw new DomainRuleException("Кампания с таким кодом не найдена.");

        if (command.Request.CharacterId is { } characterId)
        {
            var character = await db.GetOwnedAsync(command.UserId, characterId, tracking: false, ct);
            await CampaignMembership.AddCharacterAsync(db, campaign, command.UserId, character.Id, ct);
        }
        else await CampaignMembership.EnsureMemberAsync(db, campaign, command.UserId, ct);
        await db.SaveChangesAsync(ct);
        return await CampaignMapper.BuildDetailAsync(db, campaign, command.UserId, ct);
    }
}
