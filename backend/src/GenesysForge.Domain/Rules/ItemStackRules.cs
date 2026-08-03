using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Когда одинаковые предметы складываются в одну строку инвентаря, а когда остаются разными
/// экземплярами.
///
/// <para>
/// Складывать можно только то, что действительно взаимозаменяемо: строка несёт не одну ссылку на
/// каталог, а состояние экземпляра — работу, материал инструмента, повреждение, настройку руны,
/// установленные улучшения и место хранения. Две записи, отличающиеся хоть одним из этих полей,
/// остаются раздельными: иначе гномий меч и железный превратились бы в «×2», а починенный —
/// в сломанный.
/// </para>
/// </summary>
public static class ItemStackRules
{
    /// <summary>
    /// Новая покупка добавляется к существующей строке, а не создаёт вторую.
    /// </summary>
    /// <param name="existing">Строка инвентаря, к которой примеряем.</param>
    /// <param name="itemCode">Код записи каталога: у рун и настроенных инструментов свои правила.</param>
    /// <param name="state">Состояние, в котором добавляют предмет.</param>
    /// <param name="craftsmanship">Качество изготовления нового экземпляра.</param>
    /// <param name="material">Материал магического инструмента.</param>
    /// <param name="provenance">Происхождение: выдача, покупка или стартовый бюджет.</param>
    /// <param name="hasInstalledAttachments">На строке стоят улучшения.</param>
    public static bool CanStack(
        CharacterItem existing,
        string? itemCode,
        ItemState state,
        WeaponCraftsmanship craftsmanship,
        ImplementMaterial material,
        ItemProvenance provenance,
        bool hasInstalledAttachments)
    {
        // Руна — всегда отдельный экземпляр со своей настройкой (ROT-MAG-11), инструмент после
        // настройки — тоже: у них разные выбранные эффекты при одном коде.
        if (RuneboundShardRules.IsShard(itemCode) || existing.ShardConfigured) return false;
        if (ImplementRules.IsImplement(itemCode) || existing.ImplementConfigured) return false;

        // Повреждённый экземпляр чинится сам по себе, и его цена ремонта считается по нему же.
        if (existing.DamageState != ItemDamageState.Undamaged) return false;

        // Улучшения стоят на конкретном экземпляре, а метательное оружие уже брошено и лежит
        // где-то отдельно от того, что в руках.
        if (hasInstalledAttachments || existing.IsThrown) return false;

        // Груз транспорта — это место хранения: новая покупка попадает к владельцу, а не в повозку.
        if (existing.CarriedByMountId is not null || existing.IsInstalledOnMount) return false;

        // Изготовленный экземпляр несёт свои траты символов (ROT-CRAFT-01): доза с отложенным
        // действием и обычная — разные вещи, даже когда зелье в каталоге одно.
        if (existing.CraftingProjectId is not null) return false;

        // Используемый предмет занимает место — руку или корпус (ROT-EQP-01). Два меча в руках это
        // две занятые руки, а не счётчик в одной строке, поэтому в руках ничего не складывается.
        if (state == ItemState.Equipped || existing.State == ItemState.Equipped) return false;

        return existing.State == state
            && existing.Craftsmanship == craftsmanship
            && existing.ImplementMaterial == material
            && existing.Provenance == provenance;
    }
}
