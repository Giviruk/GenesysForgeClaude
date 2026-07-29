import { describe, expect, it } from 'vitest'
import type { Spell } from '../api/types'
import { magicAdvantageSpends } from './magicRoller'

const spell = (nameEn: string, nameRu = nameEn): Spell => ({
  id: nameEn, magicSkill: '', kind: 'additionalEffect', parentEffect: 'Attack',
  nameRu, nameEn, difficulty: '+1', description: '', safeDescription: '', source: 'Test',
  isCustom: false, restrictedSkill: '', repeatable: false, allowedSkills: [],
  difficultyIncrease: 1, exclusions: [], resolution: 'activatedQuality',
  isOptional: false, usesKnowledgeRating: false, ratedQualities: [],
})

describe('magic advantage spends', () => {
  it('раскладывает составные качества магической атаки на отдельные активации', () => {
    const attack = { ...spell('Attack', 'Атака'), kind: 'effect' as const }
    const options = magicAdvantageSpends(attack, [
      spell('Lightning', 'Молниеносный'),
      spell('Impact', 'Ударный'),
      spell('Destructive', 'Разрушительный'),
    ])
    expect(options.map(x => x.id)).toEqual([
      'magic-stun', 'magic-auto-fire', 'magic-knockdown', 'magic-disorient', 'magic-sunder',
    ])
    expect(options.find(x => x.id === 'magic-sunder')?.requiresSuccess).toBeUndefined()
    expect(options.find(x => x.id === 'magic-knockdown')?.costLabelRu).toContain('силуэтов')
  })

  it('добавляет восстановление усталости для Heal и пост-бросковую дополнительную цель', () => {
    const heal = { ...spell('Heal', 'Лечение'), kind: 'effect' as const }
    expect(magicAdvantageSpends(heal, [spell('Additional Target', 'Дополнительная цель')])
      .map(x => x.id)).toEqual(['magic-heal-strain', 'magic-additional-target'])
  })

  it('покрывает пост-бросковые расходы Conjure, Mask и Predict', () => {
    const conjure = { ...spell('Conjure', 'Призыв'), kind: 'effect' as const }
    expect(magicAdvantageSpends(conjure, [spell('Additional Summon')]).map(x => x.cost))
      .toEqual([1])

    const mask = { ...spell('Mask', 'Маска'), kind: 'effect' as const }
    expect(magicAdvantageSpends(mask, [spell('Additional Illusion'), spell('Realism')])
      .map(x => x.id)).toEqual(['magic-additional-illusion', 'magic-realism'])

    const predict = { ...spell('Predict', 'Предсказание'), kind: 'effect' as const }
    expect(magicAdvantageSpends(predict, [spell('Additional Questions'), spell('Empowered')])
      .map(x => x.cost)).toEqual([2, 3])
  })
})
