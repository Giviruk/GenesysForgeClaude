import { describe, expect, it } from 'vitest'
import type { ItemDef } from '../api/types'
import { catalogWeaponStats } from './itemCombatStats'

const item = (overrides: Partial<ItemDef>): ItemDef => ({
  damage: '', crit: '', attackProfiles: [],
  ...overrides,
} as ItemDef)

describe('catalogWeaponStats', () => {
  it('берёт урон и крит из структурного профиля атаки', () => {
    expect(catalogWeaponStats(item({
      damage: '', crit: '',
      attackProfiles: [{
        code: 'default', nameRu: '', nameEn: '', isDefault: true, skillName: 'Melee',
        damageKind: 'brawnPlus', damageValue: 3, crit: 2, range: 'engaged',
        cannotAttackEngaged: false, fixedDifficulty: null, qualities: [], baseDamage: null,
        poolModifiers: null,
      }],
    }))).toEqual({ damage: '+3', crit: '2' })
  })

  it('сохраняет строковые поля как fallback для старого контента', () => {
    expect(catalogWeaponStats(item({ damage: '7', crit: '3' })))
      .toEqual({ damage: '7', crit: '3' })
  })
})
