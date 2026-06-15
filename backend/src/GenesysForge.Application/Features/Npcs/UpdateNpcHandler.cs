using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Npcs;

public class UpdateNpcHandler(IAppDbContext db) : ICommandHandler<UpdateNpcCommand, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(UpdateNpcCommand command, CancellationToken ct = default)
    {
        var npc = await NpcMapper.GetOwnedAsync(db, command.UserId, command.Id, ct, tracking: true);
        NpcMapper.Apply(npc, command.Input);
        NpcValidator.Validate(npc);

        await db.SaveChangesAsync(ct);
        return NpcMapper.ToDetail(npc, command.UserId);
    }
}
