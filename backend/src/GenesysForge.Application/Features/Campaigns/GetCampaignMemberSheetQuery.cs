using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

/// <summary>Участник кампании запрашивает read-only лист одного из её персонажей.</summary>
public record GetCampaignMemberSheetQuery(Guid UserId, Guid CampaignId, Guid CharacterId)
    : IQuery<CharacterSheetDto>;
