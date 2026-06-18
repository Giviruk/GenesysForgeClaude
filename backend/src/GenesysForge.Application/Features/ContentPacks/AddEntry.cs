using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.ContentPacks;

public record AddContentPackEntryCommand(Guid UserId, Guid PackId, ContentPackEntryInput Input)
    : ICommand<ContentPackDetailDto>;

public class AddContentPackEntryHandler(IAppDbContext db)
    : ICommandHandler<AddContentPackEntryCommand, ContentPackDetailDto>
{
    public async Task<ContentPackDetailDto> Handle(AddContentPackEntryCommand command, CancellationToken ct = default)
    {
        var (pack, _) = await ContentPackMapper.GetAsGmAsync(db, command.UserId, command.PackId, ct, tracking: true);

        var entry = new ContentPackEntry { Id = Guid.NewGuid(), ContentPackId = pack.Id, Title = command.Input.Title };
        ContentPackMapper.ApplyEntry(entry, command.Input);
        entry.SortOrder = pack.Entries.Count == 0 ? 0 : pack.Entries.Max(e => e.SortOrder) + 1;
        pack.Entries.Add(entry);
        pack.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return ContentPackMapper.ToDetail(pack, isGm: true);
    }
}
