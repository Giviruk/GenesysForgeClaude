using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.ContentPacks;

public record UpdateContentPackCommand(Guid UserId, Guid Id, UpdateContentPackRequest Request)
    : ICommand<ContentPackDetailDto>;

public class UpdateContentPackHandler(IAppDbContext db)
    : ICommandHandler<UpdateContentPackCommand, ContentPackDetailDto>
{
    public async Task<ContentPackDetailDto> Handle(UpdateContentPackCommand command, CancellationToken ct = default)
    {
        var (pack, _) = await ContentPackMapper.GetAsGmAsync(db, command.UserId, command.Id, ct, tracking: true);
        var r = command.Request;

        if (r.Name is { } name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("Название Content Pack не может быть пустым.");
            pack.Name = name.Trim();
        }
        if (r.Description is { } desc) pack.Description = desc.Trim();
        if (r.System is { } system) pack.System = system;
        if (r.IsPublicToCampaign is { } pub) pack.IsPublicToCampaign = pub;
        pack.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return ContentPackMapper.ToDetail(pack, isGm: true);
    }
}
