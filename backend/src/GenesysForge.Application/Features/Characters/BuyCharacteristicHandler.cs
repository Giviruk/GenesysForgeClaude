using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

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
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
