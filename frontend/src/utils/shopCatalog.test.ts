import { describe, expect, it } from 'vitest'
import type { AttachmentDef, ItemDef, MountDef, Reference } from '../api/types'
import { buildShopProducts, productPrice, productRarity } from './shopCatalog'

function item(id: string, shopCategory: ItemDef['shopCategory']): ItemDef {
  return {
    id, code: `rot.item.${id}`, name: id, nameRu: id, kind: 'gear', encumbrance: 0,
    soakBonus: 0, meleeDefense: 0, rangedDefense: 0, encumbranceThresholdBonus: 0,
    description: '', safeDescription: '', source: '', price: 1, rarity: 1,
    skillName: '', damage: '', crit: '', rangeBand: '', properties: '', isCustom: false,
    qualities: [], hardPoints: null, checkModifiers: [], attackProfiles: [], implement: null,
    shard: null, purchasable: true, sellable: true, shopCategory,
  }
}

function attachment(id: string, hostKind: AttachmentDef['hostKind'], isEnchantment: boolean): AttachmentDef {
  return {
    id, code: id, name: id, nameRu: id, hardPointCost: 1, price: 10, rarity: 2,
    isEnchantment, hostKind, requiredTraits: '', requiredAnyTraits: '', forbiddenTraits: '',
    description: '', descriptionEn: '', source: '', effects: [],
  }
}

function mount(id: string): MountDef {
  return {
    id, code: `rot.mount.${id}`, name: id, nameRu: id, kind: 'minion',
    characteristics: { brawn: 4, agility: 2, intellect: 1, cunning: 1, willpower: 1, presence: 1 },
    soak: 4, woundThreshold: 7, strainThreshold: null, meleeDefense: 0, rangedDefense: 0,
    silhouette: 2, capacity: 18, price: 200, rarity: 1, includedGear: ['harness'],
    requiresRidingCheck: false, skills: [], abilities: [], attacks: [],
    description: '', descriptionEn: '', source: '',
  }
}

const reference = {
  archetypes: [], careers: [], skills: [], talents: [], heroicAbilities: [],
  heroicSecondaryEffects: [], qualities: [],
  items: [item('dagger', 'weaponLight'), item('wagon', 'transport'), item('ale', 'service')],
  attachments: [
    attachment('edge', 'weapon', false),
    attachment('rune', 'weapon', true),
    attachment('plate', 'armor', false),
  ],
  mounts: [mount('beast-of-burden')],
} as Reference

describe('buildShopProducts', () => {
  it('keeps server item categories and splits attachment categories', () => {
    const products = buildShopProducts(reference)
    expect(products.map(product => product.category)).toEqual([
      'weaponLight', 'transport', 'service',
      'weaponAttachment', 'weaponEnchantment', 'armorAttachment',
      // Скакун живёт в «Транспорте», но остаётся собственным типом товара (ROT-MOUNT-ITEM-01).
      'transport',
    ])
  })

  it('exposes mounts as their own product type with the mount price and rarity', () => {
    const product = buildShopProducts(reference).find(p => p.type === 'mount')

    expect(product).toBeDefined()
    expect(product!.category).toBe('transport')
    expect(productPrice(product!)).toBe(200)
    expect(productRarity(product!)).toBe(1)
  })
})
