using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CampaignMemberDto(Guid CharacterId, string CharacterName, GameSystem System,
    string Archetype, string Career, bool IsMine);
