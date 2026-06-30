using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class GetSharedCharacterSheetHandler(IAppDbContext db) : IQueryHandler<GetSharedCharacterSheetQuery, CharacterSheetDto>
{
    public async Task<CharacterSheetDto> Handle(GetSharedCharacterSheetQuery query, CancellationToken ct = default)
    {
        var raw = (query.Token ?? "").Trim();
        if (raw.Length == 0) throw new DomainRuleException("Ссылка на персонажа не найдена.");

        var hash = CharacterShareTokens.Hash(raw);
        var share = await db.CharacterShareTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct)
            ?? throw new DomainRuleException("Ссылка на персонажа не найдена или отозвана.");

        var character = await db.LoadWithRelationsAsync(share.CharacterId, tracking: false, ct)
            ?? throw new DomainRuleException("Персонаж не найден.");

        return await SheetBuilder.BuildAsync(db, character.OwnerUserId, character, ct);
    }
}
