using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.ContentPacks;

public record RemoveContentPackEntryCommand(Guid UserId, Guid PackId, Guid EntryId) : ICommand<Unit>;

public class RemoveContentPackEntryHandler(IAppDbContext db)
    : ICommandHandler<RemoveContentPackEntryCommand, Unit>
{
    public async Task<Unit> Handle(RemoveContentPackEntryCommand command, CancellationToken ct = default)
    {
        var (pack, _) = await ContentPackMapper.GetAsGmAsync(db, command.UserId, command.PackId, ct, tracking: true);
        var entry = pack.Entries.FirstOrDefault(e => e.Id == command.EntryId)
            ?? throw new DomainRuleException("Запись не найдена.");
        db.ContentPackEntries.Remove(entry);
        pack.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
