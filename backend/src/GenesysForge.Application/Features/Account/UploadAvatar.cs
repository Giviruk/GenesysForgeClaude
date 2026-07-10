using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Media;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Account;

public record UploadAvatarCommand(Guid UserId, byte[] Content) : ICommand<AccountDto>;

public class UploadAvatarHandler(IAppDbContext db, IObjectStorage storage)
    : ICommandHandler<UploadAvatarCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UploadAvatarCommand command, CancellationToken ct = default)
    {
        ImageUpload.Validate(storage, command.Content, out var contentType, out var extension);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new DomainRuleException("Пользователь не найден.");

        var previous = user.AvatarUrl;
        using var content = new MemoryStream(command.Content, writable: false);
        user.AvatarUrl = await storage.UploadPublicAsync(
            content, $"avatars/{user.Id}/{Guid.NewGuid():N}.{extension}", contentType, ct);
        await db.SaveChangesAsync(ct);

        // Прошлый файл удаляем только после успешного сохранения: иначе сбой оставит пользователя без аватара.
        await storage.DeleteByUrlAsync(previous, ct);

        return new AccountDto(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.CreatedAt);
    }
}
