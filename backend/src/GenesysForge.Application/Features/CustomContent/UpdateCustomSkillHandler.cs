using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomSkillHandler(IAppDbContext db) : ICommandHandler<UpdateCustomSkillCommand, SkillDefDto>
{
    public async Task<SkillDefDto> Handle(UpdateCustomSkillCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название навыка не может быть пустым.");

        var def = await db.SkillDefs.FirstOrDefaultAsync(
                s => s.Id == command.SkillDefId && s.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный навык не найден.");

        var name = req.Name.Trim();
        if (await db.SkillDefs.AnyAsync(s => s.Id != def.Id && s.System == req.System && s.Name == name
                && (s.OwnerUserId == null || s.OwnerUserId == command.UserId), ct))
            throw new ConflictException("Навык с таким названием уже существует в этой системе.");

        var before = def.ToDto();

        def.System = req.System;
        def.Name = name;
        def.Characteristic = req.Characteristic;
        def.Kind = req.Kind;
        CustomContentAudit.Updated(db, "skill", def.Id, def.HomebrewPackId, def.Name, command.UserId, before, def.ToDto());
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
