using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;

namespace GenesysForge.Application.Features.GameTable;

public record UpdateSessionCommand(Guid UserId, Guid CampaignId, UpdateSessionRequest Request) : ICommand<GameSessionDto>;

public class UpdateSessionHandler(IAppDbContext db) : ICommandHandler<UpdateSessionCommand, GameSessionDto>
{
    public async Task<GameSessionDto> Handle(UpdateSessionCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var session = await GameTableMapper.RequireActiveAsync(db, command.CampaignId, ct, tracking: true);
        var r = command.Request;

        if (r.Name is not null && !string.IsNullOrWhiteSpace(r.Name)) session.Name = r.Name.Trim();
        if (r.Description is not null) session.Description = r.Description.Trim();
        if (r.PublicNotes is not null) session.PublicNotes = r.PublicNotes;
        if (r.GmNotes is not null) session.GmNotes = r.GmNotes;
        // Story points не могут быть отрицательными.
        if (r.PlayerStoryPoints is { } psp) session.PlayerStoryPoints = Math.Max(0, psp);
        if (r.GmStoryPoints is { } gsp) session.GmStoryPoints = Math.Max(0, gsp);
        if (r.AllowPlayerEdits is { } allow) session.AllowPlayerEdits = allow;
        session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return GameTableMapper.ToDto(session, isGm: true);
    }
}
