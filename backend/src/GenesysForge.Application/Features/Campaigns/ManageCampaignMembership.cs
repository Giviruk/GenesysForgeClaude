using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public record AddCampaignCharacterRequest(Guid CharacterId);
public record AddCampaignCharacterCommand(Guid UserId, Guid CampaignId, Guid CharacterId) : ICommand<CampaignDetailDto>;
public record RemoveCampaignMemberCommand(Guid UserId, Guid CampaignId, Guid MemberUserId) : ICommand<Unit>;

public class AddCampaignCharacterHandler(IAppDbContext db) : ICommandHandler<AddCampaignCharacterCommand, CampaignDetailDto>
{
    public async Task<CampaignDetailDto> Handle(AddCampaignCharacterCommand command, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        var character = await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: false, ct);
        await CampaignMembership.AddCharacterAsync(db, campaign, command.UserId, character.Id, ct);
        await db.SaveChangesAsync(ct);
        return await CampaignMapper.BuildDetailAsync(db, campaign, command.UserId, ct);
    }
}

public class RemoveCampaignMemberHandler(IAppDbContext db) : ICommandHandler<RemoveCampaignMemberCommand, Unit>
{
    public async Task<Unit> Handle(RemoveCampaignMemberCommand command, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        if (command.MemberUserId == campaign.GmUserId)
            throw new DomainRuleException("Мастер не может покинуть кампанию или быть исключён.");
        if (command.UserId != command.MemberUserId && command.UserId != campaign.GmUserId)
            throw new DomainRuleException("Исключать игроков может только мастер.");
        var member = await db.CampaignMembers.FirstOrDefaultAsync(
            m => m.CampaignId == campaign.Id && m.UserId == command.MemberUserId, ct)
            ?? throw new DomainRuleException("Игрок не найден в кампании.");
        var links = await db.CampaignCharacters.Where(
            cc => cc.CampaignId == campaign.Id && cc.PlayerUserId == command.MemberUserId).ToListAsync(ct);
        db.CampaignCharacters.RemoveRange(links);
        db.CampaignMembers.Remove(member);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
