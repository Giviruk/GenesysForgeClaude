using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Campaigns;

public record UpdateCampaignNoteCommand(Guid UserId, Guid CampaignId, Guid NoteId, SaveCampaignNoteRequest Request)
    : ICommand<CampaignNoteDto>;
