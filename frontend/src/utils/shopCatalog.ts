import type { AttachmentDef, ItemDef, Reference, ShopItemCategory } from '../api/types'

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
  return [...items, ...attachments]
}

export const productPrice = (product: ShopProduct): number | null =>
  product.type === 'item' ? product.item.price : product.attachment.price

export const productRarity = (product: ShopProduct): number | null =>
  product.type === 'item' ? product.item.rarity : product.attachment.rarity
