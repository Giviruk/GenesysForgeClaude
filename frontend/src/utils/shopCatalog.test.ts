import { describe, expect, it } from 'vitest'
import type { AttachmentDef, ItemDef, Reference } from '../api/types'
import { buildShopProducts } from './shopCatalog'

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

const reference = {
  archetypes: [], careers: [], skills: [], talents: [], heroicAbilities: [],
  heroicSecondaryEffects: [], qualities: [],
  items: [item('dagger', 'weaponLight'), item('wagon', 'transport'), item('ale', 'service')],
  attachments: [
    attachment('edge', 'weapon', false),
    attachment('rune', 'weapon', true),
    attachment('plate', 'armor', false),
  ],
} as Reference

describe('buildShopProducts', () => {
  it('keeps server item categories and splits attachment categories', () => {
    const products = buildShopProducts(reference)
    expect(products.map(product => product.category)).toEqual([
      'weaponLight', 'transport', 'service',
      'weaponAttachment', 'weaponEnchantment', 'armorAttachment',
    ])
  })
})
