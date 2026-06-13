using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomHeroicAbilityHandler(IAppDbContext db)
    : ICommandHandler<UpdateCustomHeroicAbilityCommand, HeroicAbilityDto>
{
    public async Task<HeroicAbilityDto> Handle(UpdateCustomHeroicAbilityCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название способности не может быть пустым.");

        var def = await db.HeroicAbilityDefs.FirstOrDefaultAsync(
                h => h.Id == command.HeroicAbilityId && h.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомная героическая способность не найдена.");

        def.Name = req.Name.Trim();
        def.Description = req.Description ?? "";
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
