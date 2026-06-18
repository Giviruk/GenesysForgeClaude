using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.ContentPacks;

public record DeleteContentPackCommand(Guid UserId, Guid Id) : ICommand<Unit>;

public class DeleteContentPackHandler(IAppDbContext db) : ICommandHandler<DeleteContentPackCommand, Unit>
{
    public async Task<Unit> Handle(DeleteContentPackCommand command, CancellationToken ct = default)
    {
        var (pack, _) = await ContentPackMapper.GetAsGmAsync(db, command.UserId, command.Id, ct, tracking: true);
        db.ContentPacks.Remove(pack);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
