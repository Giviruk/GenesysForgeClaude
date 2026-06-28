import { describe, expect, it } from 'vitest'
import type { ItemDef, NpcDetail, Reference, SkillDef } from '../api/types'
import type { NpcAttackEntry } from '../api/types'
import {
  attackFromItem, buildPool, npcAttackViews, npcSkillViews, skillIndex, splitEquipment,
  syncAttacksWithEquipment, weaponsByLabel,
} from './npcStats'

function skillDef(p: Partial<SkillDef> & Pick<SkillDef, 'name' | 'nameRu' | 'characteristic'>): SkillDef {
  return { id: p.name, kind: 'combat', safeDescription: '', source: '', isCustom: false, ...p }
}

function itemDef(p: Partial<ItemDef> & Pick<ItemDef, 'name' | 'nameRu' | 'kind'>): ItemDef {
  return {
    id: p.name, encumbrance: 0, soakBonus: 0, meleeDefense: 0, rangedDefense: 0,
    encumbranceThresholdBonus: 0, description: '', safeDescription: '', source: '', price: 0,
    rarity: 0, skillName: '', damage: '', crit: '', rangeBand: '', properties: '', isCustom: false,
    qualities: [], ...p,
  }
}

const reference: Reference = {
  archetypes: [], careers: [], talents: [], heroicAbilities: [], qualities: [],
  skills: [
    skillDef({ name: 'Melee', nameRu: 'Ближний бой', characteristic: 'brawn' }),
    skillDef({ name: 'Ranged (Light)', nameRu: 'Дальний бой (лёгкое)', characteristic: 'agility' }),
  ],
  items: [
    itemDef({ name: 'Sword', nameRu: 'Меч', kind: 'weapon', skillName: 'Melee', damage: '+2', crit: '2', rangeBand: 'Ближняя', properties: 'Точное 1' }),
    itemDef({ name: 'Pistol', nameRu: 'Пистолет', kind: 'weapon', skillName: 'Ranged (Light)', damage: '6', crit: '3', rangeBand: 'Средняя' }),
    itemDef({ name: 'Robes', nameRu: 'Роба', kind: 'armor' }),
  ],
}

const npc: NpcDetail = {
  id: 'n1', name: 'Гоблин', system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute',
  description: '', source: '', brawn: 3, agility: 2, intellect: 2, cunning: 2, willpower: 1, presence: 1,
  woundThreshold: 12, strainThreshold: 12, soak: 4, meleeDefense: 0, rangedDefense: 0,
  silhouette: 1, tactics: '',
  visibility: 'private', campaignId: null, isMine: true, isBuiltIn: false,
  skills: [{ name: 'Ближний бой', ranks: 2 }],
  abilities: [], attacks: [], talents: [], equipment: ['Меч', 'Пистолет', 'Роба'], tags: [],
  warnings: [],
  createdAt: '', updatedAt: '',
}

describe('buildPool — пул проверки навыка', () => {
  it('улучшает min(хар, ранги) кубов до Proficiency', () => {
    expect(buildPool(3, 2)).toEqual({ ability: 1, proficiency: 2 })
    expect(buildPool(2, 4)).toEqual({ ability: 2, proficiency: 2 })
    expect(buildPool(3, 0)).toEqual({ ability: 3, proficiency: 0 })
    expect(buildPool(0, 0)).toEqual({ ability: 0, proficiency: 0 })
  })
})

describe('npcSkillViews — пулы навыков NPC', () => {
  it('считает пул по характеристике навыка из справочника', () => {
    const views = npcSkillViews(npc, skillIndex(reference))
    expect(views).toHaveLength(1)
    expect(views[0]).toMatchObject({ name: 'Ближний бой', ranks: 2, characteristic: 'brawn' })
    // Мощь 3, ранги 2 → 2 жёлтых + 1 зелёный
    expect(views[0].pool).toEqual({ ability: 1, proficiency: 2 })
  })

  it('оставляет пул null для навыка вне справочника', () => {
    const custom: NpcDetail = { ...npc, skills: [{ name: 'Запугивание чем-то', ranks: 1 }] }
    expect(npcSkillViews(custom, skillIndex(reference))[0].pool).toBeNull()
  })
})

describe('splitEquipment — оружие vs прочее', () => {
  it('сопоставляет оружие, раскрывает урон «+N» как Мощь, прочее уводит в gear', () => {
    const { weapons, gear } = splitEquipment(npc, reference)
    expect(gear.map(g => g.name)).toEqual(['Роба'])
    expect(gear[0].item?.kind).toBe('armor') // привязка к каталогу для описания/бонусов
    expect(weapons.map(w => w.name)).toEqual(['Меч', 'Пистолет'])

    const sword = weapons[0]
    expect(sword.damageText).toBe('5 (Мощь +2)') // Мощь 3 + 2
    expect(sword.skillLabel).toBe('Ближний бой')
    expect(sword.pool).toEqual({ ability: 1, proficiency: 2 }) // Мощь 3, 2 ранга

    const pistol = weapons[1]
    expect(pistol.damageText).toBe('6')
    // NPC без рангов в «Дальний бой (лёгкое)» → пул = только характеристика (Ловкость 2)
    expect(pistol.pool).toEqual({ ability: 2, proficiency: 0 })
  })

  it('без справочника всё снаряжение считается прочим', () => {
    const { weapons, gear } = splitEquipment(npc, null)
    expect(weapons).toEqual([])
    expect(gear.map(g => g.name)).toEqual(['Меч', 'Пистолет', 'Роба'])
    expect(gear.every(g => g.item === null)).toBe(true)
  })
})

describe('npcAttackViews — структурные атаки NPC', () => {
  const withAttacks: NpcDetail = {
    ...npc,
    attacks: [{
      name: 'Длинный меч', skillName: 'Melee', damage: '+2', critical: '2', rangeBand: 'Ближняя', notes: '',
      qualities: [{ qualityCode: 'precise', nameRu: 'Точное', rating: 1 }], sourceWeapon: '',
    }],
  }

  it('считает пул по навыку атаки и раскрывает урон «+N» как Мощь', () => {
    const [a] = npcAttackViews(withAttacks, reference)
    expect(a.skillLabel).toBe('Ближний бой')
    expect(a.pool).toEqual({ ability: 1, proficiency: 2 }) // Мощь 3, 2 ранга
    expect(a.damageText).toBe('5 (Мощь +2)')
    expect(a.crit).toBe('2')
    expect(a.qualities[0].label).toBe('Точное 1')
  })

  it('без справочника пул null, абсолютные поля сохраняются', () => {
    const [a] = npcAttackViews(withAttacks, null)
    expect(a.pool).toBeNull()
    expect(a.skillLabel).toBeNull()
    expect(a.damageText).toBe('5 (Мощь +2)') // раскрытие урона не зависит от справочника
  })
})

describe('syncAttacksWithEquipment — атаки из снаряжения', () => {
  const wbl = weaponsByLabel(reference)
  const custom: NpcAttackEntry = {
    name: 'Плевок', skillName: 'Ranged (Light)', damage: '4', critical: '5', rangeBand: 'Средняя',
    notes: '', qualities: [], sourceWeapon: '',
  }

  it('оружие в снаряжении создаёт производную атаку, броня — нет', () => {
    const result = syncAttacksWithEquipment(['Меч', 'Роба'], [custom], wbl)
    expect(result).toHaveLength(2) // производная (Меч) + кастом
    const derived = result.find(a => a.sourceWeapon === 'Меч')!
    expect(derived.damage).toBe('+2')           // из каталога
    expect(derived.skillName).toBe('Melee')
    expect(derived.name).toBe('Меч')
    expect(result).toContain(custom)            // кастомная сохранена
    expect(result.some(a => a.sourceWeapon === 'Роба')).toBe(false) // броня не оружие
  })

  it('убрали оружие → производная атака исчезла, кастом остался', () => {
    const withSword = syncAttacksWithEquipment(['Меч'], [custom], wbl)
    const afterRemoval = syncAttacksWithEquipment([], withSword, wbl)
    expect(afterRemoval).toEqual([custom])
  })

  it('правки производной атаки сохраняются при ресинке (ключ — подпись оружия)', () => {
    const first = syncAttacksWithEquipment(['Меч'], [], wbl)
    const edited = first.map(a => ({ ...a, notes: 'целит в ноги' }))
    const resynced = syncAttacksWithEquipment(['Меч'], edited, wbl)
    expect(resynced[0].notes).toBe('целит в ноги')
  })

  it('attackFromItem помечает источник подписью оружия', () => {
    const sword = reference.items.find(i => i.nameRu === 'Меч')!
    expect(attackFromItem(sword).sourceWeapon).toBe('Меч')
  })
})
