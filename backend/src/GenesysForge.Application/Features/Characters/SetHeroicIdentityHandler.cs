using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Заполняет личное название и происхождение героической способности (ROT-HA-01).
/// Запрос проверяется целиком до первой записи; частично применённая личность невозможна.
/// </summary>
public class SetHeroicIdentityHandler(IAppDbContext db) : ICommandHandler<SetHeroicIdentityCommand, Unit>
{
    public async Task<Unit> Handle(SetHeroicIdentityCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        HeroicIdentityGate.EnsureEditable(c);

        var req = command.Request;
        var mode = req.OriginMode ?? c.HeroicOriginMode;
        if (mode is null)
            throw new DomainRuleException(
                "Выберите категорию происхождения, опишите её или бросьте по таблице.",
                "heroic.identity.origin_required");

        // Поля происхождения принимаются только вместе с явным режимом: иначе сохранённый бросок
        // молча превратился бы в выбор вручную, а его грани — в недостоверный аудит.
        var keepStoredOrigin = req.OriginMode is null;
        var primary = keepStoredOrigin ? c.HeroicOriginPrimary : req.OriginPrimary;
        var secondary = keepStoredOrigin ? c.HeroicOriginSecondary : req.OriginSecondary;
        var narrative = req.OriginNarrative ?? (keepStoredOrigin ? c.HeroicOriginNarrative : null);
        var rolls = keepStoredOrigin ? HeroicIdentityRules.ParseRolls(c.HeroicOriginRolls) : [];

        var identity = HeroicIdentityRules.Validate(
            req.CustomName, mode.Value, primary, secondary, narrative, rolls);

        var wasIncomplete = c.HeroicIdentityIncomplete;

        c.HeroicCustomName = identity.CustomName;
        c.HeroicOriginMode = identity.OriginMode;
        c.HeroicOriginPrimary = identity.OriginPrimary;
        c.HeroicOriginSecondary = identity.OriginSecondary;
        c.HeroicOriginNarrative = identity.OriginNarrative;
        c.HeroicOriginRolls = HeroicIdentityRules.FormatRolls(identity.OriginRolls);

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.HeroicIdentitySet,
            $"Героическая способность: «{identity.CustomName}»",
            data: new
            {
                customName = identity.CustomName,
                originMode = identity.OriginMode.ToString(),
                originPrimary = identity.OriginPrimary?.ToString(),
                originSecondary = identity.OriginSecondary?.ToString(),
                rolls = identity.OriginRolls,
                repairedLegacy = wasIncomplete && !c.IsCreationPhase,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
