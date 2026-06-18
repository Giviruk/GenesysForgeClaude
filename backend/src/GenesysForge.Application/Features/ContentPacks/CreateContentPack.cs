using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.ContentPacks;

public record CreateContentPackCommand(Guid UserId, Guid CampaignId, CreateContentPackRequest Request)
    : ICommand<ContentPackDetailDto>;

public class CreateContentPackHandler(IAppDbContext db)
    : ICommandHandler<CreateContentPackCommand, ContentPackDetailDto>
{
    public async Task<ContentPackDetailDto> Handle(CreateContentPackCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        if (string.IsNullOrWhiteSpace(command.Request.Name))
            throw new DomainRuleException("Название Content Pack не может быть пустым.");

        var pack = new ContentPack
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.UserId,
            CampaignId = command.CampaignId,
            Name = command.Request.Name.Trim(),
            Description = command.Request.Description?.Trim() ?? "",
            System = command.Request.System,
        };
        db.ContentPacks.Add(pack);
        await db.SaveChangesAsync(ct);
        return ContentPackMapper.ToDetail(pack, isGm: true);
    }
}
