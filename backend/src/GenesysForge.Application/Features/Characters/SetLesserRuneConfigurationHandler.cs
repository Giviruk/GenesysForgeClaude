using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public record SetLesserRuneConfigurationCommand(
    Guid UserId,
    Guid CharacterId,
    Guid CharacterItemId,
    SetLesserRuneConfigurationRequest Request) : ICommand<Unit>;

/// <summary>Одноразовая настройка экземпляра Lesser Rune (ROT-MAG-11).</summary>
public class SetLesserRuneConfigurationHandler(IAppDbContext db)
    : ICommandHandler<SetLesserRuneConfigurationCommand, Unit>
{
    public async Task<Unit> Handle(
        SetLesserRuneConfigurationCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.CharacterItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.", "item.not_found");
        var spec = RuneboundShardRules.For(item.ItemDef?.Code);
        if (spec is not { NeedsConfiguration: true })
            throw new DomainRuleException(
                "Этот предмет не является ненастроенной Lesser Rune.",
                "shard.configuration.not_supported");
        if (item.ShardConfigured)
            throw new DomainRuleException(
                "Lesser Rune уже настроена; изменить выбор можно только отдельным GM repair.",
                "shard.configuration.immutable");

        var activation = command.Request.ActivationDescription?.Trim() ?? "";
        if (activation.Length is < 3 or > 500)
            throw new DomainRuleException(
                "Опишите небольшой activation effect текстом от 3 до 500 символов.",
                "shard.configuration.activation_invalid");

        var action = command.Request.ActionCode?.Trim() ?? "";
        var effect = command.Request.EffectCode?.Trim() ?? "";
        var spell = await db.SpellDefs.AsNoTracking().FirstOrDefaultAsync(s =>
            s.System == c.System
            && s.Kind == SpellEntryKind.AdditionalEffect
            && s.ParentEffect == action
            && s.NameEn == effect, ct);
        if (spell is null)
            throw new DomainRuleException(
                "Выбранный дополнительный эффект не найден.",
                "shard.configuration.effect_not_found");
        if (spell.DifficultyIncrease != 1)
            throw new DomainRuleException(
                "Lesser Rune выбирает эффект с печатной надбавкой ровно +1.",
                "shard.configuration.effect_must_cost_one");
        if (!MagicMatrix.SkillsForEffect(action, effect)
                .Contains(RuneboundShardRules.RequiredMagicSkill, StringComparer.Ordinal))
            throw new DomainRuleException(
                "Этот эффект нельзя применять с Runes.",
                "shard.configuration.effect_not_available_for_runes");

        item.ShardActivationChoice = activation;
        item.ShardEffectAction = action;
        item.ShardEffectChoice = effect;
        item.ShardConfigured = true;

        var name = item.ItemDef?.NameRu ?? item.ItemDef?.Name ?? "Lesser Rune";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ShardConfigured,
            $"Настроена «{name}»: {action}/{effect}",
            null,
            new
            {
                characterItemId = item.Id,
                activation,
                action,
                effect,
                difficultyIncrease = spell.DifficultyIncrease,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
