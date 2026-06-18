using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Encounters;

public record GetEncounterQuery(Guid UserId, Guid Id) : IQuery<EncounterDetailDto>;

public class GetEncounterHandler(IAppDbContext db) : IQueryHandler<GetEncounterQuery, EncounterDetailDto>
{
    public async Task<EncounterDetailDto> Handle(GetEncounterQuery q, CancellationToken ct = default)
    {
        var (encounter, isGm) = await EncounterMapper.GetViewableAsync(db, q.UserId, q.Id, ct);
        return EncounterMapper.ToDetail(encounter, isGm);
    }
}
