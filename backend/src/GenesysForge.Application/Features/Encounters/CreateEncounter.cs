using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Encounters;

public record CreateEncounterCommand(Guid UserId, Guid CampaignId, EncounterInput Input) : ICommand<EncounterDetailDto>;

public class CreateEncounterHandler(IAppDbContext db) : ICommandHandler<CreateEncounterCommand, EncounterDetailDto>
{
    public async Task<EncounterDetailDto> Handle(CreateEncounterCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);

        var encounter = new Encounter
        {
            Id = Guid.NewGuid(),
            CampaignId = command.CampaignId,
            Name = command.Input.Name,
        };
        EncounterMapper.Apply(encounter, command.Input);

        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(ct);
        return EncounterMapper.ToDetail(encounter, isGm: true);
    }
}
