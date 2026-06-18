using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Encounters;

public record UpdateEncounterCommand(Guid UserId, Guid Id, EncounterInput Input) : ICommand<EncounterDetailDto>;

public class UpdateEncounterHandler(IAppDbContext db) : ICommandHandler<UpdateEncounterCommand, EncounterDetailDto>
{
    public async Task<EncounterDetailDto> Handle(UpdateEncounterCommand command, CancellationToken ct = default)
    {
        var (encounter, _) = await EncounterMapper.GetAsGmAsync(db, command.UserId, command.Id, ct, tracking: true);
        EncounterMapper.Apply(encounter, command.Input);
        await db.SaveChangesAsync(ct);
        return EncounterMapper.ToDetail(encounter, isGm: true);
    }
}
