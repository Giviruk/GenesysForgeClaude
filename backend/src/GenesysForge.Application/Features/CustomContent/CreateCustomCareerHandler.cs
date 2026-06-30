using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class CreateCustomCareerHandler(IAppDbContext db) : ICommandHandler<CreateCustomCareerCommand, CareerDto>
{
    public async Task<CareerDto> Handle(CreateCustomCareerCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var careerSkills = await CustomCareerValidator.ValidateAndNormalizeSkillsAsync(db, command.UserId, req, ct);
        var name = req.Name.Trim();

        if (await db.CareerDefs.AnyAsync(c => c.System == req.System && c.Name == name
                && (c.OwnerUserId == null || c.OwnerUserId == command.UserId), ct))
            throw new ConflictException("Карьера с таким названием уже существует в этой системе.");

        var def = new CareerDef
        {
            Id = Guid.NewGuid(),
            System = req.System,
            Name = name,
            NameRu = string.IsNullOrWhiteSpace(req.NameRu) ? name : req.NameRu.Trim(),
            Description = req.Description?.Trim() ?? "",
            SafeDescription = req.Description?.Trim() ?? "",
            Source = "Custom",
            OwnerUserId = command.UserId,
            CareerSkillNames = careerSkills,
            StartingMoneyFixed = req.StartingMoneyFixed,
            StartingMoneyDice = req.StartingMoneyDice?.Trim() ?? "",
        };
        db.CareerDefs.Add(def);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
