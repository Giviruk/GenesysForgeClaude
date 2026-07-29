using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Настройка магического инструмента ведущим (ROT-MAG-IMP-01).</summary>
public record SetImplementConfigurationCommand(
    Guid UserId, Guid CharacterId, Guid CharacterItemId, SetImplementConfigurationRequest Request)
    : ICommand<Unit>;

/// <summary>
/// Фолиант и палочка получают свои бесплатные эффекты не из витрины, а от ведущего: выбор делается
/// один раз, когда экземпляр изготовлен или впервые попал в руки, и дальше не меняется. До этого
/// обычные числа инструмента работают, а бесплатный эффект — нет.
///
/// Коды эффектов проверяются по справочнику заклинаний: выдумать «бесплатный Усиленный за +0»
/// нельзя, надбавку берёт сервер из той же таблицы, что показывает сборщик заклинаний.
/// </summary>
public class SetImplementConfigurationHandler(IAppDbContext db)
    : ICommandHandler<SetImplementConfigurationCommand, Unit>
{
    public async Task<Unit> Handle(
        SetImplementConfigurationCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.CharacterItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.", "item.not_found");

        var spec = ImplementRules.For(item.ItemDef?.Code)
            ?? throw new DomainRuleException(
                "Этот предмет не является магическим инструментом.", "implement.not_an_implement");

        var codes = (req.EffectCodes ?? [])
            .Select(x => x?.Trim() ?? "")
            .Where(x => x.Length > 0)
            .ToList();

        // Надбавки берутся из справочника, а не из запроса: клиент называет только коды.
        var effects = await db.SpellDefs.AsNoTracking()
            .Where(s => s.System == c.System && s.Kind == SpellEntryKind.AdditionalEffect)
            .ToListAsync(ct);
        var chosen = new List<SpellEffectInput>();
        foreach (var code in codes)
        {
            var def = effects.FirstOrDefault(e => string.Equals(e.NameEn, code, StringComparison.Ordinal))
                ?? throw new DomainRuleException(
                    $"Эффект «{code}» не найден в справочнике магии.", "implement.effect.not_found");
            chosen.Add(new SpellEffectInput(def.NameEn, def.DifficultyIncrease, def.RestrictedSkill));
        }

        ImplementRules.EnsureConfigurationValid(spec, chosen, req.OverrideReason);

        item.ImplementChoices = string.Join(",", chosen.Select(x => x.Code));
        item.ImplementConfigured = true;

        var name = item.ItemDef?.NameRu ?? item.ItemDef?.Name ?? "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ImplementConfigured,
            $"Настроен инструмент «{name}»"
            + (chosen.Count > 0 ? $": {string.Join(", ", chosen.Select(x => x.Code))}" : ""),
            null,
            new
            {
                characterItemId = item.Id, name, implement = spec.Code,
                effects = chosen.Select(x => x.Code).ToList(),
                increaseSum = chosen.Sum(x => x.Increase),
                overrideReason = req.OverrideReason,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
