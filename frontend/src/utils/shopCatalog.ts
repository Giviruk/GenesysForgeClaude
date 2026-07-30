import type { AttachmentDef, ItemDef, MountDef, Reference, ShopItemCategory } from '../api/types'

export type ShopCategory =
  | 'all'
  | ShopItemCategory
  | 'weaponAttachment'
  | 'weaponEnchantment'
  | 'armorAttachment'

export type ShopProduct =
  | { key: string; type: 'item'; category: ShopItemCategory; item: ItemDef }
  | {
      key: string
      type: 'attachment'
      category: 'weaponAttachment' | 'weaponEnchantment' | 'armorAttachment'
      attachment: AttachmentDef
    }
  // Скакун — не запись снаряжения, а существо со статблоком (ROT-MOUNT-ITEM-01): в витрине он
  // живёт в «Транспорте», но покупается своей командой и не становится позицией инвентаря.
  | { key: string; type: 'mount'; category: 'transport'; mount: MountDef }

export function buildShopProducts(reference: Reference): ShopProduct[] {
  const items: ShopProduct[] = reference.items.map(item => ({
    key: `item:${item.id}`,
    type: 'item',
    category: item.shopCategory,
    item,
  }))
  const attachments: ShopProduct[] = (reference.attachments ?? []).map(attachment => ({
    key: `attachment:${attachment.id}`,
    type: 'attachment',
    category: attachment.hostKind === 'armor'
      ? 'armorAttachment'
      : attachment.isEnchantment ? 'weaponEnchantment' : 'weaponAttachment',
    attachment,
  }))
  const mounts: ShopProduct[] = (reference.mounts ?? []).map(mount => ({
    key: `mount:${mount.id}`,
    type: 'mount',
    category: 'transport',
    mount,
  }))
  return [...items, ...attachments, ...mounts]
}

export const productPrice = (product: ShopProduct): number | null =>
  product.type === 'item' ? product.item.price
    : product.type === 'attachment' ? product.attachment.price
      : product.mount.price

export const productRarity = (product: ShopProduct): number | null =>
  product.type === 'item' ? product.item.rarity
    : product.type === 'attachment' ? product.attachment.rarity
      : product.mount.rarity
