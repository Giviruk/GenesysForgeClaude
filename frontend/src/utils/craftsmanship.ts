import type { ItemKind, WeaponCraftsmanship } from '../api/types'

/**
 * Качество изготовления бывает только у оружия и брони (ROT-WPN-02): мешок эльфийским не бывает,
 * и выбор для снаряжения не показывается.
 */
export const craftsmanshipApplies = (kind: ItemKind) => kind === 'weapon' || kind === 'armor'

/**
 * Цена экземпляра для предпросмотра покупки. Источник истины — сервер: он пересчитывает сумму
 * по этому же правилу и списывает её сам. Здесь оно повторено только чтобы в магазине сразу
 * было видно, во что обойдётся выбор, — как и остальная арифметика витрины.
 */
export function craftsmanshipPrice(basePrice: number, craftsmanship: WeaponCraftsmanship): number {
  switch (craftsmanship) {
    case 'iron': return Math.floor(basePrice / 2)
    case 'dwarven':
    case 'elven': return basePrice * 2
    case 'ancient': return basePrice * 20
    default: return basePrice
  }
}

/** Редкость экземпляра для предпросмотра; сервер повторно считает её при покупке. */
export function craftsmanshipRarity(baseRarity: number, craftsmanship: WeaponCraftsmanship): number {
  switch (craftsmanship) {
    case 'iron': return Math.max(0, baseRarity - 1)
    case 'dwarven': return Math.min(10, baseRarity + 2)
    case 'elven': return Math.min(10, baseRarity + 3)
    case 'ancient': return 10
    default: return Math.min(10, Math.max(0, baseRarity))
  }
}
