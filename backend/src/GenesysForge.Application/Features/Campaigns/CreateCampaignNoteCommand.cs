using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public record CreateCampaignNoteCommand(Guid UserId, Guid CampaignId, SaveCampaignNoteRequest Request)
    : ICommand<CampaignNoteDto>;
