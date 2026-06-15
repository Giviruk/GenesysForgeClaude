using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Npcs;

public class QuickDraftNpcHandler(IAppDbContext db) : ICommandHandler<QuickDraftNpcCommand, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(QuickDraftNpcCommand command, CancellationToken ct = default)
    {
        var r = command.Request;
        var npc = NpcDraftGenerator.Generate(command.UserId,
            new NpcDraftRequest(r.System, r.Kind, r.Role, r.PowerLevel, r.PrimaryCharacteristic, r.CombatStyle, r.Name));
        NpcValidator.Validate(npc);

        db.Npcs.Add(npc);
        await db.SaveChangesAsync(ct);
        return NpcMapper.ToDetail(npc, command.UserId);
    }
}
