using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Media;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public record UploadCharacterPortraitCommand(Guid UserId, Guid CharacterId, byte[] Content) : ICommand<string>;

/// <summary>Загружает портрет персонажа. Возвращает публичный URL изображения.</summary>
public class UploadCharacterPortraitHandler(IAppDbContext db, IObjectStorage storage)
    : ICommandHandler<UploadCharacterPortraitCommand, string>
{
    public async Task<string> Handle(UploadCharacterPortraitCommand command, CancellationToken ct = default)
    {
        ImageUpload.Validate(storage, command.Content, out var contentType, out var extension);

        // Персонаж ищется вместе с владельцем: чужой портрет подменить нельзя.
        var character = await db.Characters
            .FirstOrDefaultAsync(c => c.Id == command.CharacterId && c.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Персонаж не найден.");

        var previous = character.PortraitUrl;
        using var content = new MemoryStream(command.Content, writable: false);
        character.PortraitUrl = await storage.UploadPublicAsync(
            content, $"portraits/{character.Id}/{Guid.NewGuid():N}.{extension}", contentType, ct);
        await db.SaveChangesAsync(ct);

        // Прошлый файл удаляем только после успешного сохранения.
        await storage.DeleteByUrlAsync(previous, ct);

        return character.PortraitUrl;
    }
}
