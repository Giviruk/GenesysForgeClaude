using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Campaigns;

public record DeleteCampaignNoteCommand(Guid UserId, Guid CampaignId, Guid NoteId) : ICommand<Unit>;
