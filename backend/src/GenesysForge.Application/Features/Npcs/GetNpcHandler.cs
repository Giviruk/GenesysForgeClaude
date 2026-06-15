using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

public class GetNpcHandler(IAppDbContext db) : IQueryHandler<GetNpcQuery, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(GetNpcQuery query, CancellationToken ct = default)
    {
        var npc = await NpcMapper.GetViewableAsync(db, query.UserId, query.Id, ct);
        return NpcMapper.ToDetail(npc, query.UserId);
    }
}
