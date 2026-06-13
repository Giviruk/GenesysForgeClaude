using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Campaigns;

public record RemoveCampaignCharacterCommand(Guid UserId, Guid CampaignId, Guid CharacterId) : ICommand<Unit>;
