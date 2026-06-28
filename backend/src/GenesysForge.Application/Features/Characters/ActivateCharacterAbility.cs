using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Результат активации способности на листе: обновлённый лист + что применилось/осталось вручную.</summary>
public record ActivateCharacterAbilityResult(
    CharacterSheetDto Sheet, string AbilityName, IReadOnlyList<string> Applied, IReadOnlyList<string> Manual);

public record ActivateCharacterAbilityCommand(Guid UserId, Guid CharacterId)
    : ICommand<ActivateCharacterAbilityResult>;

/// <summary>
/// Активирует героическую способность персонажа на листе (U-18 Stage 2): применяет структурные эффекты
/// к живому состоянию (current раны/усталость персистятся; пороги/soak/защита — производные, отражаются в
/// результате как применённые на сцену). Пишет запись в audit-log (U-09). Boost/story/manual → подсказки.
/// </summary>
public class ActivateCharacterAbilityHandler(IAppDbContext db)
    : ICommandHandler<ActivateCharacterAbilityCommand, ActivateCharacterAbilityResult>
{
    public async Task<ActivateCharacterAbilityResult> Handle(
        ActivateCharacterAbilityCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: true, ct);
        if (c.HeroicAbilityId is null)
            throw new DomainRuleException("У персонажа нет героической способности для активации.");

        var effects = await db.RuleEffectDefs.AsNoTracking()
            .Where(e => e.HeroicAbilityDefId == c.HeroicAbilityId).ToListAsync(ct);

        var sheet = await SheetBuilder.BuildAsync(db, command.UserId, c, ct);
        var target = new MutableCombatTarget
        {
            WoundsCurrent = c.WoundsCurrent,
            WoundsThreshold = sheet.Derived.WoundThreshold,
            StrainCurrent = c.StrainCurrent,
            StrainThreshold = sheet.Derived.StrainThreshold,
            Soak = sheet.Derived.Soak,
            MeleeDefense = sheet.Derived.MeleeDefense,
            RangedDefense = sheet.Derived.RangedDefense,
        };

        var result = RuleEffectApplier.Apply(effects, target);
        // Персистим только current раны/усталость (пороги/soak у листа производные — на сцену учитываются вручную).
        c.WoundsCurrent = target.WoundsCurrent;
        c.StrainCurrent = target.StrainCurrent;

        var name = c.HeroicAbility is { } h
            ? (string.IsNullOrWhiteSpace(h.NameRu) ? h.Name : h.NameRu) : "Способность";
        var summary = result.Applied.Count > 0
            ? $"Активирована «{name}»: {string.Join("; ", result.Applied)}"
            : $"Активирована «{name}»";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.AbilityActivated,
            Trim(summary, 400), null, new { applied = result.Applied, manual = result.Manual });

        await db.SaveChangesAsync(ct);

        var updated = await SheetBuilder.BuildAsync(db, command.UserId, c, ct);
        return new ActivateCharacterAbilityResult(updated, name, result.Applied, result.Manual);
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
