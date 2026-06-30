using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomArchetypeHandler(IAppDbContext db) : ICommandHandler<UpdateCustomArchetypeCommand, ArchetypeDto>
{
    public async Task<ArchetypeDto> Handle(UpdateCustomArchetypeCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        CustomArchetypeValidator.Validate(req);

        var def = await db.ArchetypeDefs
                .Include(a => a.Abilities)
                .Include(a => a.StartingSkills)
                .FirstOrDefaultAsync(a => a.Id == command.ArchetypeId && a.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный архетип не найден.");

        var name = req.Name.Trim();
        if (await db.ArchetypeDefs.AnyAsync(a => a.Id != def.Id && a.System == req.System && a.Name == name
                && (a.OwnerUserId == null || a.OwnerUserId == command.UserId), ct))
            throw new ConflictException("Архетип с таким названием уже существует в этой системе.");

        def.System = req.System;
        def.Name = name;
        def.NameRu = string.IsNullOrWhiteSpace(req.NameRu) ? name : req.NameRu.Trim();
        def.Brawn = req.Brawn;
        def.Agility = req.Agility;
        def.Intellect = req.Intellect;
        def.Cunning = req.Cunning;
        def.Willpower = req.Willpower;
        def.Presence = req.Presence;
        def.WoundBase = req.WoundBase;
        def.StrainBase = req.StrainBase;
        def.StartingXp = req.StartingXp;
        def.Description = req.Description?.Trim() ?? "";
        def.SafeDescription = req.Description?.Trim() ?? "";
        def.Source = "Custom";

        db.ArchetypeAbilityDefs.RemoveRange(def.Abilities.ToList());
        def.Abilities.Clear();
        CreateCustomArchetypeHandler.AddAbility(def, req);
        foreach (var ability in def.Abilities)
            db.ArchetypeAbilityDefs.Add(ability);

        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
