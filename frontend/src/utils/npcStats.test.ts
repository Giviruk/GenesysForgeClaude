import { describe, expect, it } from 'vitest'
import type { ItemDef, NpcDetail, Reference, SkillDef } from '../api/types'
import type { NpcAttackEntry } from '../api/types'
import {
  attackFromItem, buildPool, npcAttackViews, npcSkillLabel, npcSkillName, npcSkillViews, skillIndex, splitEquipment,
  syncAttacksWithEquipment, weaponsByLabel,
} from './npcStats'
import { adversaryMarkdown } from '../components/print/markdown'

function skillDef(p: Partial<SkillDef> & Pick<SkillDef, 'name' | 'nameRu' | 'characteristic'>): SkillDef {
  return { id: p.name, kind: 'combat', safeDescription: '', source: '', isCustom: false, ...p }
}

function itemDef(p: Partial<ItemDef> & Pick<ItemDef, 'name' | 'nameRu' | 'kind'>): ItemDef {
  return {
    id: p.name, code: p.name.toLowerCase(),
    shopCategory: p.kind === 'weapon' ? 'weaponLight' : p.kind,
    encumbrance: 0,
    soakBonus: 0, meleeDefense: 0, rangedDefense: 0,
    encumbranceThresholdBonus: 0, description: '', safeDescription: '', source: '', price: 0,
    rarity: 0, skillName: '', damage: '', crit: '', rangeBand: '', properties: '', isCustom: false,
    qualities: [], hardPoints: null, checkModifiers: [], attackProfiles: [], implement: null,
    shard: null, purchasable: true, sellable: true, ...p,
  }
}

const reference: Reference = {
  archetypes: [], careers: [], talents: [], heroicAbilities: [], heroicSecondaryEffects: [], qualities: [],
  attachments: [], mounts: [],
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
  it('resolves old Russian skill names and displays the book name without changing ranks', () => {
    const ref = { ...reference, skills: [skillDef({ name: 'Brawl', nameRu: 'Грубая сила', characteristic: 'brawn' })] }
    const saved: NpcDetail = { ...npc, skills: [{ name: 'Рукопашный бой', ranks: 2 }],
      attacks: [{ name: 'Кулаки', skillName: 'Brawl', damage: '+0', critical: '5', rangeBand: 'Engaged', notes: '', qualities: [], sourceWeapon: '' }] }
    expect(npcSkillViews(saved, skillIndex(ref))[0]).toMatchObject({ name: 'Грубая сила', ranks: 2, pool: { ability: 1, proficiency: 2 } })
    expect(npcSkillName('Рукопашный бой', skillIndex(ref))).toBe('Brawl')
    expect(npcAttackViews(saved, ref)[0].pool).toEqual({ ability: 1, proficiency: 2 })
    const markdown = adversaryMarkdown(saved, 'gm', ref)
    expect(markdown).toContain('Грубая сила 2')
    expect(markdown).toContain('навык Грубая сила')
    expect(markdown).toContain('Вплотную')
    expect(markdown).not.toContain('Рукопашный бой')
  })

  it('prefers a custom skill exact name over a built-in legacy alias', () => {
    const custom = skillDef({ name: 'Внимательность', nameRu: 'Особый навык', characteristic: 'intellect', isCustom: true })
    const builtin = skillDef({ name: 'Perception', nameRu: 'Внимание', characteristic: 'cunning' })
    const index = skillIndex({ ...reference, skills: [custom, builtin] })
    expect(index.get('Внимательность')).toBe(custom)
    expect(npcSkillLabel('Внимательность', index)).toBe('Особый навык')
    expect(npcSkillLabel('Неизвестный навык', index)).toBe('Неизвестный навык')
    expect(npcSkillName('Внимательность', index)).toBe('Внимательность')
  })

  it('keeps distinct row keys when multiple saved skills resolve to the same display label', () => {
    const ref = { skills: [skillDef({ name: 'Brawl', nameRu: 'Грубая сила', characteristic: 'brawn' })] }
    const views = npcSkillViews({ ...npc, skills: [{ name: 'Brawl', ranks: 1 }, { name: 'Рукопашный бой', ranks: 2 }] }, skillIndex(ref))
    expect(views.map(s => s.name)).toEqual(['Грубая сила', 'Грубая сила'])
    expect(new Set(views.map(s => s.key)).size).toBe(2)
    expect(views.map(s => s.ranks)).toEqual([1, 2])
  })

  it('uses the same split-to-base attack skill fallback in cards and Markdown', () => {
    const split = { ...npc, attacks: [{ name: 'Меч', skillName: 'Melee (Light)', damage: '+3', critical: '2',
      rangeBand: 'Engaged', notes: '', qualities: [], sourceWeapon: '' }] }
    expect(npcAttackViews(split, reference)[0].skillLabel).toBe('Ближний бой')
    expect(adversaryMarkdown(split, 'gm', reference)).toContain('навык Ближний бой')
    expect(adversaryMarkdown(split, 'gm', reference)).not.toContain('Melee (Light)')
  })

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

  it('для группы миньонов использует ранг количество миньонов минус один', () => {
    const minion: NpcDetail = { ...npc, kind: 'minion', strainThreshold: null,
      skills: [{ name: 'Ближний бой', ranks: 0 }] }
    const [view] = npcSkillViews(minion, skillIndex(reference), 4)

    expect(view.ranks).toBe(3)
    expect(view.pool).toEqual({ ability: 0, proficiency: 3 })
  })
})

describe('splitEquipment — оружие vs прочее', () => {
  it('сопоставляет оружие, раскрывает урон «+N» как Мощь, прочее уводит в gear', () => {
    const { weapons, gear } = splitEquipment(npc, reference)
    expect(gear.map(g => g.name)).toEqual(['Роба'])
    expect(gear[0].item?.kind).toBe('armor') // привязка к каталогу для описания/бонусов
    expect(weapons.map(w => w.name)).toEqual(['Меч', 'Пистолет'])

    const sword = weapons[0]
    expect(sword.damageText).toBe('5 (Сила +2)') // Мощь 3 + 2
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
    expect(a.damageText).toBe('5 (Сила +2)')
    expect(a.crit).toBe('2')
    expect(a.qualities[0].label).toBe('Точное 1')
  })

  it('без справочника пул null, абсолютные поля сохраняются', () => {
    const [a] = npcAttackViews(withAttacks, null)
    expect(a.pool).toBeNull()
    expect(a.skillLabel).toBeNull()
    expect(a.damageText).toBe('5 (Сила +2)') // раскрытие урона не зависит от справочника
  })

  it('масштабирует атаку миньонов только когда её навык входит в групповые', () => {
    const minion: NpcDetail = { ...withAttacks, kind: 'minion', strainThreshold: null,
      skills: [{ name: 'Ближний бой', ranks: 0 }] }
    expect(npcAttackViews(minion, reference, 3)[0].pool).toEqual({ ability: 1, proficiency: 2 })

    const withoutGroupSkill: NpcDetail = { ...minion, skills: [] }
    expect(npcAttackViews(withoutGroupSkill, reference, 3)[0].pool).toEqual({ ability: 3, proficiency: 0 })
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
