using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.ContentPacks;

public record GetContentPackQuery(Guid UserId, Guid Id) : IQuery<ContentPackDetailDto>;

public class GetContentPackHandler(IAppDbContext db) : IQueryHandler<GetContentPackQuery, ContentPackDetailDto>
{
    public async Task<ContentPackDetailDto> Handle(GetContentPackQuery q, CancellationToken ct = default)
    {
        var (pack, isGm) = await ContentPackMapper.GetViewableAsync(db, q.UserId, q.Id, ct);
        return ContentPackMapper.ToDetail(pack, isGm);
    }
}
