using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Читает только названные части листа. Вкладка берёт свою и не платит за чужие: инвентарь — это
/// две трети веса листа, а главной вкладке он не нужен вовсе.
/// </summary>
public record GetCharacterSlicesQuery(Guid UserId, Guid CharacterId, SheetSlice Slices)
    : IQuery<SheetSlicesDto>;

public class GetCharacterSlicesHandler(IAppDbContext db)
    : IQueryHandler<GetCharacterSlicesQuery, SheetSlicesDto>
{
    public async Task<SheetSlicesDto> Handle(GetCharacterSlicesQuery query, CancellationToken ct = default)
    {
        var character = await db.GetOwnedSlicesAsync(query.UserId, query.CharacterId, query.Slices, ct);
        return await SheetBuilder.BuildSlicesAsync(db, query.UserId, character, query.Slices, ct);
    }
}
