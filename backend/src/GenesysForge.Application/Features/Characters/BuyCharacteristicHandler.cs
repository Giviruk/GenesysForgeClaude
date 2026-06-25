using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

public class BuyCharacteristicHandler(IAppDbContext db) : ICommandHandler<BuyCharacteristicCommand, Unit>
{
    public async Task<Unit> Handle(BuyCharacteristicCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var current = c.GetCharacteristic(command.Characteristic);

        var result = PurchaseValidator.BuyCharacteristic(current, c.AvailableXp, c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        c.IncreaseCharacteristic(command.Characteristic);
        c.SpentXp += result.Cost;

        var label = CharacterAudit.CharacteristicLabel(command.Characteristic);
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.CharacteristicBought,
            $"Куплена характеристика «{label}» ({current}→{current + 1})", -result.Cost,
            new { characteristic = command.Characteristic.ToString(), from = current, to = current + 1, cost = result.Cost });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
