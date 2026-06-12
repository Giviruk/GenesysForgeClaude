using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public class GetCharacterSheetHandler(IAppDbContext db) : IQueryHandler<GetCharacterSheetQuery, CharacterSheetDto>
{
    public async Task<CharacterSheetDto> Handle(GetCharacterSheetQuery query, CancellationToken ct = default)
    {
        var character = await db.GetOwnedAsync(query.UserId, query.CharacterId, tracking: false, ct);
        return await SheetBuilder.BuildAsync(db, query.UserId, character, ct);
    }
}
