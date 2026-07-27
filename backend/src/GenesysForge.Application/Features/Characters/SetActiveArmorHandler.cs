using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Выбирает активную броню (ROT-CMB-02). <c>null</c> снимает выбор: тогда броня не даёт защиты вовсе.
/// </summary>
public record SetActiveArmorCommand(Guid UserId, Guid CharacterId, Guid? CharacterItemId) : ICommand<Unit>;

public class SetActiveArmorHandler(IAppDbContext db) : ICommandHandler<SetActiveArmorCommand, Unit>
{
    public async Task<Unit> Handle(SetActiveArmorCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);

        if (command.CharacterItemId is { } itemId)
        {
            // Чужая, отсутствующая, ненадетая позиция или не-броня выбираться не может.
            var item = c.Items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new DomainRuleException("Предмет не найден у персонажа.", "armor.item_not_found");
            if (item.ItemDef?.Kind != ItemKind.Armor)
                throw new DomainRuleException("Активной может быть только броня.", "armor.not_armor");
            if (item.State != ItemState.Equipped)
                throw new DomainRuleException("Активной может быть только надетая броня.", "armor.not_equipped");
            if (item.Quantity < 1)
                throw new DomainRuleException("У позиции нет ни одного экземпляра.", "armor.no_quantity");

            c.ActiveArmorCharacterItemId = item.Id;
        }
        else
        {
            c.ActiveArmorCharacterItemId = null;
        }

        var name = command.CharacterItemId is null
            ? null
            : c.Items.First(i => i.Id == command.CharacterItemId).ItemDef?.NameRu;
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ActiveArmorChanged,
            name is null ? "Активная броня снята" : $"Активная броня: «{name}»",
            data: new { characterItemId = command.CharacterItemId, name });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
