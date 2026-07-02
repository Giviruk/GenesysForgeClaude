using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>
/// Live preview быстрого черновика: генерирует NPC той же логикой, что и создание
/// (<see cref="QuickDraftNpcHandler"/>), но ничего не сохраняет — форма показывает результат до подтверждения.
/// </summary>
public class PreviewQuickDraftNpcHandler(IAppDbContext db) : IQueryHandler<PreviewQuickDraftNpcQuery, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(PreviewQuickDraftNpcQuery query, CancellationToken ct = default)
    {
        var npc = await QuickDraftNpcHandler.BuildDraftAsync(db, query.UserId, query.Request, ct);
        return NpcMapper.ToDetail(npc, query.UserId);
    }
}
