using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Account;

public record GetAccountQuery(Guid UserId) : IQuery<AccountDto>;

public class GetAccountHandler(IAppDbContext db) : IQueryHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountQuery query, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == query.UserId, ct)
            ?? throw new DomainRuleException("Пользователь не найден.");
        return new AccountDto(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.CreatedAt);
    }
}
