using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

/// <summary>GM запрашивает read-only лист персонажа участника своей кампании (U-20).</summary>
public record GetCampaignMemberSheetQuery(Guid GmUserId, Guid CampaignId, Guid CharacterId)
    : IQuery<CharacterSheetDto>;
