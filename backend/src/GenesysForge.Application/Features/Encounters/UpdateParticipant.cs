using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Encounters;

public record UpdateEncounterParticipantCommand(
    Guid UserId, Guid EncounterId, Guid ParticipantId, UpdateEncounterParticipantRequest Request)
    : ICommand<EncounterDetailDto>;

public class UpdateEncounterParticipantHandler(IAppDbContext db)
    : ICommandHandler<UpdateEncounterParticipantCommand, EncounterDetailDto>
{
    public async Task<EncounterDetailDto> Handle(UpdateEncounterParticipantCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.EncounterId, ct, tracking: true);

        var p = encounter.Participants.FirstOrDefault(x => x.Id == command.ParticipantId)
            ?? throw new DomainRuleException("Участник не найден.");

        var r = command.Request;
        if (r.DisplayName is { } name && !string.IsNullOrWhiteSpace(name)) p.DisplayName = name.Trim();
        if (r.InitiativeSide is { } side) p.InitiativeSide = side;
        if (r.Quantity is { } qty) p.Quantity = Math.Max(1, qty);
        if (r.Notes is { } notes) p.Notes = notes.Trim();
        if (r.StartsHidden is { } hidden) p.StartsHidden = hidden;
        if (r.StartsDefeated is { } defeated) p.StartsDefeated = defeated;
        // 0 трактуем как «снять переопределение» — отрицательные значения недопустимы.
        if (r.StartingWoundsOverride is { } wt) p.StartingWoundsOverride = wt > 0 ? wt : null;
        if (r.StartingStrainOverride is { } st) p.StartingStrainOverride = st > 0 ? st : null;

        encounter.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return EncounterMapper.ToDetail(encounter, isGm: true);
    }
}
