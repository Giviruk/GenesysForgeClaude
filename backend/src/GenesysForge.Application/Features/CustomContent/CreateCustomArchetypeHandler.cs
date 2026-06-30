using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class CreateCustomArchetypeHandler(IAppDbContext db) : ICommandHandler<CreateCustomArchetypeCommand, ArchetypeDto>
{
    public async Task<ArchetypeDto> Handle(CreateCustomArchetypeCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        CustomArchetypeValidator.Validate(req);
        var name = req.Name.Trim();

        if (await db.ArchetypeDefs.AnyAsync(a => a.System == req.System && a.Name == name
                && (a.OwnerUserId == null || a.OwnerUserId == command.UserId), ct))
            throw new ConflictException("Архетип с таким названием уже существует в этой системе.");

        var def = new ArchetypeDef
        {
            Id = Guid.NewGuid(),
            System = req.System,
            Name = name,
            NameRu = string.IsNullOrWhiteSpace(req.NameRu) ? name : req.NameRu.Trim(),
            Brawn = req.Brawn,
            Agility = req.Agility,
            Intellect = req.Intellect,
            Cunning = req.Cunning,
            Willpower = req.Willpower,
            Presence = req.Presence,
            WoundBase = req.WoundBase,
            StrainBase = req.StrainBase,
            StartingXp = req.StartingXp,
            Description = req.Description?.Trim() ?? "",
            SafeDescription = req.Description?.Trim() ?? "",
            Source = "Custom",
            OwnerUserId = command.UserId,
        };

        AddAbility(def, req);
        db.ArchetypeDefs.Add(def);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }

    internal static void AddAbility(ArchetypeDef def, CreateCustomArchetypeRequest req)
    {
        var abilityName = req.AbilityNameRu?.Trim();
        var abilityDescription = req.AbilityDescription?.Trim();
        if (string.IsNullOrWhiteSpace(abilityName) && string.IsNullOrWhiteSpace(abilityDescription)) return;

        def.Abilities.Add(new ArchetypeAbilityDef
        {
            Id = Guid.NewGuid(),
            ArchetypeId = def.Id,
            Code = $"custom.archetype.{def.Id:N}.ability",
            NameRu = abilityName ?? "Особенность",
            NameEn = "",
            SafeDescription = abilityDescription ?? "",
            AutomationKind = ArchetypeAbilityAutomationKind.Manual,
        });
    }
}
