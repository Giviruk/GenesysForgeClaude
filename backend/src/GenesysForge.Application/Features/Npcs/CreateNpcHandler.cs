using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Npcs;

public class CreateNpcHandler(IAppDbContext db) : ICommandHandler<CreateNpcCommand, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(CreateNpcCommand command, CancellationToken ct = default)
    {
        var npc = new Npc { Id = Guid.NewGuid(), OwnerUserId = command.UserId, Name = command.Input.Name };
        NpcMapper.Apply(npc, command.Input);
        await NpcMapper.ResolveAttackQualitiesAsync(db, npc, ct);
        NpcValidator.Validate(npc);

        db.Npcs.Add(npc);
        await db.SaveChangesAsync(ct);
        return NpcMapper.ToDetail(npc, command.UserId);
    }
}
