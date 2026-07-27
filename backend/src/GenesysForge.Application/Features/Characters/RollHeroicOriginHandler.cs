using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Бросает происхождение героической способности по таблице d10 (ROT-HA-01). Бросок выполняет
/// сервер через инъецированный RNG и сохраняет фактические грани: клиент не может прислать
/// «выпавший» результат.
/// </summary>
public class RollHeroicOriginHandler(IAppDbContext db, IDiceRoller dice)
    : ICommandHandler<RollHeroicOriginCommand, HeroicOriginRollDto>
{
    public async Task<HeroicOriginRollDto> Handle(RollHeroicOriginCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        HeroicIdentityGate.EnsureEditable(c);

        var roll = HeroicOriginTable.Roll(dice.Roll);

        c.HeroicOriginMode = roll.Mode;
        c.HeroicOriginPrimary = roll.Primary;
        c.HeroicOriginSecondary = roll.Secondary;
        c.HeroicOriginRolls = HeroicIdentityRules.FormatRolls(roll.Rolls);

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.HeroicOriginRolled,
            $"Происхождение героической способности брошено: {string.Join(", ", roll.Rolls)}",
            data: new
            {
                rolls = roll.Rolls,
                originMode = roll.Mode.ToString(),
                originPrimary = roll.Primary.ToString(),
                originSecondary = roll.Secondary?.ToString(),
            });

        await db.SaveChangesAsync(ct);
        return new HeroicOriginRollDto([.. roll.Rolls], roll.Mode, roll.Primary, roll.Secondary);
    }
}
