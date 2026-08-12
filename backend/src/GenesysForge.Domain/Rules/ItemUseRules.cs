using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Что с предметом вообще можно делать: брать в руки или надевать, и ломать.
///
/// <para>
/// Раньше «используется» предлагалось всему подряд, включая верёвку и провизию, а состояние
/// поломки — вплоть до рациона. Ни то, ни другое ничего не значило: у верёвки в руках нет ни
/// бонусов, ни занятой руки в правилах, а чинить провизию не за что.
/// </para>
/// </summary>
public static class ItemUseRules
{
    /// <summary>
    /// Предмет можно взять в руки или надеть.
    ///
    /// <para>
    /// Оружие и броня — по виду. Из снаряжения используется то, что при этом что-то даёт: рюкзак
    /// поднимает порог веса только надетым, магический инструмент и руна работают только в руках,
    /// а реликвия — только надетой (ROT-MITEM-01). Верёвка, рацион и факел без эффектов остаются
    /// «носит» и «в рюкзаке».
    /// </para>
    /// </summary>
    public static bool CanBeEquipped(ItemDef def) =>
        def.Kind is ItemKind.Weapon or ItemKind.Armor
        || ImplementRules.IsImplement(def.Code)
        || RuneboundShardRules.IsShard(def.Code)
        || MagicItemRules.IsMagicItem(def.Code)
        || def.EncumbranceThresholdBonus != 0
        || def.SoakBonus != 0
        || def.MeleeDefense != 0
        || def.RangedDefense != 0
        || def.CheckModifiers.Count > 0;

    /// <summary>
    /// Предмет можно повредить и починить (GEN-EQP-DMG-01). Ступени состояния описаны для того,
    /// что несут в бою: у оружия отваливается урон, у брони — поглощение, а магический инструмент
    /// перестаёт помогать проверкам. Контейнер книга называет отдельно — серьёзно повреждённый
    /// теряет прибавку к порогу веса, но не содержимое. Мотку верёвки и рациону ломаться нечем,
    /// и «порог поломки» у них был просто шумом на карточке.
    /// </summary>
    public static bool CanBeDamaged(ItemDef def) =>
        def.Kind is ItemKind.Weapon or ItemKind.Armor
        || ImplementRules.IsImplement(def.Code)
        || def.EncumbranceThresholdBonus != 0;
}
