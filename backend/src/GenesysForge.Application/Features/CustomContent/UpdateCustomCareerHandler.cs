using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomCareerHandler(IAppDbContext db) : ICommandHandler<UpdateCustomCareerCommand, CareerDto>
{
    public async Task<CareerDto> Handle(UpdateCustomCareerCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var careerSkills = await CustomCareerValidator.ValidateAndNormalizeSkillsAsync(db, command.UserId, req, ct);

        var def = await db.CareerDefs
                .Include(c => c.StartingGear)
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Id == command.CareerId && c.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомная карьера не найдена.");

        var name = req.Name.Trim();
        if (await db.CareerDefs.AnyAsync(c => c.Id != def.Id && c.System == req.System && c.Name == name
                && (c.OwnerUserId == null || c.OwnerUserId == command.UserId), ct))
            throw new ConflictException("Карьера с таким названием уже существует в этой системе.");

        def.System = req.System;
        def.Name = name;
        def.NameRu = string.IsNullOrWhiteSpace(req.NameRu) ? name : req.NameRu.Trim();
        def.Description = req.Description?.Trim() ?? "";
        def.SafeDescription = req.Description?.Trim() ?? "";
        def.Source = "Custom";
        def.CareerSkillNames = careerSkills;
        def.StartingMoneyFixed = req.StartingMoneyFixed;
        def.StartingMoneyDice = req.StartingMoneyDice?.Trim() ?? "";
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
