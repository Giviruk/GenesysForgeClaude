using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class AddCriticalInjuryHandler(IAppDbContext db) : ICommandHandler<AddCriticalInjuryCommand, Guid>
{
    public async Task<Guid> Handle(AddCriticalInjuryCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var r = command.Request;

        string nameRu;
        string? severity = Clean(r.Severity);
        string? ruleCode = Clean(r.RuleCode);

        if (ruleCode is not null)
        {
            // Привязка к строке таблицы крит-ранений (U-11): снимаем название/тяжесть из справочника.
            var entry = await db.RuleTableEntries.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Kind == RuleTableKind.CriticalInjury && e.Code == ruleCode, ct)
                ?? throw new DomainRuleException("Крит-ранение не найдено в справочнике.");
            nameRu = entry.NameRu;
            severity ??= string.IsNullOrWhiteSpace(entry.GroupRu) ? null : entry.GroupRu;
        }
        else
        {
            nameRu = Clean(r.NameRu)
                ?? throw new DomainRuleException("Укажите крит-ранение из справочника или название вручную.");
        }

        var injury = new CharacterCriticalInjury
        {
            Id = Guid.NewGuid(),
            CharacterId = c.Id,
            RuleCode = ruleCode,
            NameRu = nameRu,
            Severity = severity,
            RollResult = r.RollResult,
            Notes = Clean(r.Notes),
        };
        db.CharacterCriticalInjuries.Add(injury);
        c.CriticalInjuries.Add(injury);

        await db.SaveChangesAsync(ct);
        return injury.Id;
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
