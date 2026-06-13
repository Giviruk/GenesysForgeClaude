using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public class CreateCampaignHandler(IAppDbContext db) : ICommandHandler<CreateCampaignCommand, CampaignDetailDto>
{
    public async Task<CampaignDetailDto> Handle(CreateCampaignCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Name))
            throw new DomainRuleException("Название кампании не может быть пустым.");

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            GmUserId = command.UserId,
            Name = command.Request.Name.Trim(),
            Description = command.Request.Description ?? "",
            JoinCode = await GenerateUniqueCodeAsync(ct),
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);
        return await CampaignMapper.BuildDetailAsync(db, campaign, command.UserId, ct);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await db.Campaigns.AnyAsync(c => c.JoinCode == code, ct))
                return code;
        }
        throw new DomainRuleException("Не удалось сгенерировать код присоединения, попробуйте ещё раз.");
    }
}
