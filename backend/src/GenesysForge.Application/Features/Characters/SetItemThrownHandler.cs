using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Отмечает метательное оружие брошенным или подобранным (ROT-WPN-01). Limited Ammo метательного
/// профиля означает недоступность экземпляра до возврата, а не его уничтожение: топорик лежит там,
/// куда улетел, и подобрать его — отдельное действие игрока.
/// </summary>
public record SetItemThrownCommand(Guid UserId, Guid CharacterId, Guid CharacterItemId, bool IsThrown)
    : ICommand<Unit>;

public class SetItemThrownHandler(IAppDbContext db) : ICommandHandler<SetItemThrownCommand, Unit>
{
    public async Task<Unit> Handle(SetItemThrownCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);

        var item = c.Items.FirstOrDefault(i => i.Id == command.CharacterItemId)
            ?? throw new DomainRuleException("Предмет не найден у персонажа.", "item.not_found");

        // Бросить можно только то, у чего есть метательный профиль: иначе «метнуть латы» было бы
        // законной операцией, а вернуть их — отдельной загадкой.
        if (command.IsThrown && !CanBeThrown(item))
            throw new DomainRuleException(
                "У этого оружия нет метательного профиля.", "weapon.profile.not_throwable");

        if (item.IsThrown == command.IsThrown)
        {
            // Повторный вызов ничего не меняет и ошибкой не является.
            return Unit.Value;
        }

        item.IsThrown = command.IsThrown;

        // Брошенная активная броня невозможна, но брошенное оружие могло быть надето: снимаем выбор,
        // если он вдруг указывал на него.
        if (command.IsThrown && c.ActiveArmorCharacterItemId == item.Id)
            c.ActiveArmorCharacterItemId = null;

        var name = item.ItemDef?.NameRu ?? item.ItemDef?.Name ?? "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemThrownChanged,
            command.IsThrown ? $"Метнул «{name}»" : $"Подобрал «{name}»",
            data: new { characterItemId = item.Id, name, isThrown = command.IsThrown });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static bool CanBeThrown(CharacterItem item) =>
        item.ItemDef?.AttackProfiles.Any(p =>
            p.Code == WeaponProfileRules.ThrownCode
            || (p.DamageKind == DamageKind.BrawnPlus && p.Range > WeaponRange.Engaged)) == true;
}
