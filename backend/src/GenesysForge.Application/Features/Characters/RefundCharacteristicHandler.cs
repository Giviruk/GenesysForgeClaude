using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class RefundCharacteristicHandler(IAppDbContext db) : ICommandHandler<RefundCharacteristicCommand, Unit>
{
    public async Task<Unit> Handle(RefundCharacteristicCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var archetypeBase = new CharacteristicsSet(
            c.Archetype!.Brawn, c.Archetype.Agility, c.Archetype.Intellect,
            c.Archetype.Cunning, c.Archetype.Willpower, c.Archetype.Presence);

        var result = PurchaseValidator.RefundCharacteristic(
            c.GetCharacteristic(command.Characteristic),
            archetypeBase.Get(command.Characteristic),
            c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        c.DecreaseCharacteristic(command.Characteristic);
        c.SpentXp -= result.Cost;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
