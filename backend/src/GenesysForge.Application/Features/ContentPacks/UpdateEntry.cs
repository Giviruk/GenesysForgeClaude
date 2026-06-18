using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.ContentPacks;

public record UpdateContentPackEntryCommand(Guid UserId, Guid PackId, Guid EntryId, ContentPackEntryInput Input)
    : ICommand<ContentPackDetailDto>;

public class UpdateContentPackEntryHandler(IAppDbContext db)
    : ICommandHandler<UpdateContentPackEntryCommand, ContentPackDetailDto>
{
    public async Task<ContentPackDetailDto> Handle(UpdateContentPackEntryCommand command, CancellationToken ct = default)
    {
        var (pack, _) = await ContentPackMapper.GetAsGmAsync(db, command.UserId, command.PackId, ct, tracking: true);
        var entry = pack.Entries.FirstOrDefault(e => e.Id == command.EntryId)
            ?? throw new DomainRuleException("Запись не найдена.");

        ContentPackMapper.ApplyEntry(entry, command.Input);
        pack.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ContentPackMapper.ToDetail(pack, isGm: true);
    }
}
