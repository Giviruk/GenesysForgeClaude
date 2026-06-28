using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

/// <summary>
/// Отдаёт мастеру read-only лист персонажа участника кампании (U-20). Доступ — только GM кампании,
/// и только если персонаж действительно в ней состоит. Лист строится под владельца-игрока, чтобы
/// его кастомный контент (навыки) разрешался корректно.
/// </summary>
public class GetCampaignMemberSheetHandler(IAppDbContext db)
    : IQueryHandler<GetCampaignMemberSheetQuery, CharacterSheetDto>
{
    public async Task<CharacterSheetDto> Handle(GetCampaignMemberSheetQuery query, CancellationToken ct = default)
    {
        // Проверка роли GM кампании (бросает, если запрашивающий не мастер).
        await CampaignMapper.GetAsGmAsync(db, query.GmUserId, query.CampaignId, ct);

        // Персонаж должен состоять в этой кампании; заодно узнаём владельца-игрока.
        var member = await db.CampaignCharacters.AsNoTracking()
            .FirstOrDefaultAsync(cc => cc.CampaignId == query.CampaignId && cc.CharacterId == query.CharacterId, ct)
            ?? throw new DomainRuleException("Персонаж не найден в кампании.");

        var character = await db.LoadWithRelationsAsync(query.CharacterId, tracking: false, ct)
            ?? throw new DomainRuleException("Персонаж не найден в кампании.");

        return await SheetBuilder.BuildAsync(db, member.PlayerUserId, character, ct);
    }
}
