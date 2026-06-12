using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class CreateCustomSkillHandler(IAppDbContext db) : ICommandHandler<CreateCustomSkillCommand, SkillDefDto>
{
    public async Task<SkillDefDto> Handle(CreateCustomSkillCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название навыка не может быть пустым.");

        var name = req.Name.Trim();
        if (await db.SkillDefs.AnyAsync(s => s.System == req.System && s.Name == name
                && (s.OwnerUserId == null || s.OwnerUserId == command.UserId), ct))
            throw new ConflictException("Навык с таким названием уже существует в этой системе.");

        var def = new SkillDef
        {
            Id = Guid.NewGuid(), System = req.System, Name = name,
            Characteristic = req.Characteristic, Kind = req.Kind, OwnerUserId = command.UserId,
        };
        db.SkillDefs.Add(def);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
