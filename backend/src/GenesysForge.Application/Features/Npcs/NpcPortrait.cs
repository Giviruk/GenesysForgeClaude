using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Media;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Npcs;

public record UploadNpcPortraitCommand(Guid UserId, Guid NpcId, byte[] Content) : ICommand<string>;
public record DeleteNpcPortraitCommand(Guid UserId, Guid NpcId) : ICommand<Unit>;

public class UploadNpcPortraitHandler(IAppDbContext db, IObjectStorage storage)
    : ICommandHandler<UploadNpcPortraitCommand, string>
{
    public async Task<string> Handle(UploadNpcPortraitCommand command, CancellationToken ct = default)
    {
        var npc = await NpcMapper.GetOwnedAsync(db, command.UserId, command.NpcId, ct, tracking: true);
        if (npc.IsBuiltIn) throw new DomainRuleException("Встроенный NPC доступен только для чтения.");
        ImageUpload.Validate(storage, command.Content, out var contentType, out var extension);

        var previous = npc.PortraitUrl;
        using var content = new MemoryStream(command.Content, writable: false);
        npc.PortraitUrl = await storage.UploadPublicAsync(
            content, $"npc-portraits/{npc.Id}/{Guid.NewGuid():N}.{extension}", contentType, ct);
        npc.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        // Удаляем старый файл только после успешного сохранения нового URL.
        await storage.DeleteByUrlAsync(previous, ct);
        return npc.PortraitUrl;
    }
}

public class DeleteNpcPortraitHandler(IAppDbContext db, IObjectStorage storage)
    : ICommandHandler<DeleteNpcPortraitCommand, Unit>
{
    public async Task<Unit> Handle(DeleteNpcPortraitCommand command, CancellationToken ct = default)
    {
        var npc = await NpcMapper.GetOwnedAsync(db, command.UserId, command.NpcId, ct, tracking: true);
        if (npc.IsBuiltIn) throw new DomainRuleException("Встроенный NPC доступен только для чтения.");
        var previous = npc.PortraitUrl;
        npc.PortraitUrl = null;
        npc.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await storage.DeleteByUrlAsync(previous, ct);
        return Unit.Value;
    }
}
