using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

public class UpdateItemHandler(IAppDbContext db) : ICommandHandler<UpdateItemCommand, Unit>
{
    public async Task<Unit> Handle(UpdateItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");
        if (req.Quantity is < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");

        // Начало использования проверяется до изменения состояния: больше одной брони не носят,
        // а двуручное оружие занимает обе руки (ROT-EQP-01).
        if (req.State == ItemState.Equipped && item.State != ItemState.Equipped && item.ItemDef is not null)
            EquipmentSlotRules.EnsureCanEquip(
                item.ItemDef.Kind, item.ItemDef.FormTraits,
                CharacterDerived.EquippedInputs(c, item.Id));

        if (req.State is not null) item.State = req.State.Value;
        // Снятая броня перестаёт быть активной; надетая ею становится — она единственная (ROT-CMB-02).
        if (item.State != ItemState.Equipped && c.ActiveArmorCharacterItemId == item.Id)
            c.ActiveArmorCharacterItemId = null;
        else if (item.State == ItemState.Equipped && item.ItemDef?.Kind == ItemKind.Armor)
            c.ActiveArmorCharacterItemId = item.Id;
        if (req.Quantity is not null) item.Quantity = req.Quantity.Value;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
