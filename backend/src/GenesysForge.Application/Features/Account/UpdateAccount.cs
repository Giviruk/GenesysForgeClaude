using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Account;

public record UpdateAccountCommand(Guid UserId, UpdateAccountRequest Request) : ICommand<AccountDto>;

public class UpdateAccountHandler(IAppDbContext db) : ICommandHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountCommand command, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new DomainRuleException("Пользователь не найден.");

        var req = command.Request;
        if (req.DisplayName is not null)
        {
            var name = req.DisplayName.Trim();
            if (name.Length == 0) throw new DomainRuleException("Имя пользователя не может быть пустым.");
            user.DisplayName = name;
        }
        if (req.AvatarUrl is not null)
        {
            // Пустая строка очищает аватар; иначе сохраняем URL как есть.
            var url = req.AvatarUrl.Trim();
            user.AvatarUrl = url.Length == 0 ? null : url;
        }

        await db.SaveChangesAsync(ct);
        return new AccountDto(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.CreatedAt);
    }
}
