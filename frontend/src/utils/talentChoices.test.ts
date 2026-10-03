import { describe, expect, it } from 'vitest'
import type { Characteristic, CharacterTalentChoice, SheetTalent, SkillDef, SkillKind, TalentDef } from '../api/types'
import {
  choiceCountForNextRank, choiceLabel, parseSignatureSpell, signatureSpellLabel, signatureSpellValue,
  talentChoiceOptions,
} from './talentChoices'

const talent = (fields: Partial<TalentDef>) => ({
  choiceKind: 'none', choiceCountFirstRank: 0, choiceCountNextRank: 0, choiceDistinctAcrossRanks: false,
  choiceAllowedSkillKinds: [], grantsCharacteristic: false, ...fields,
} as unknown as TalentDef)

const owned = (choices: Array<Pick<CharacterTalentChoice, 'kind' | 'value'>>, granted: Characteristic[] = []) =>
  ({ choices, grantedCharacteristics: granted } as unknown as SheetTalent)

const skill = (name: string, kind: SkillKind, nameRu = name): SkillDef =>
  ({ name, nameRu, kind } as unknown as SkillDef)

const characteristics: Record<Characteristic, number> = {
  brawn: 2, agility: 3, intellect: 5, cunning: 2, willpower: 2, presence: 4,
}

describe('choiceCountForNextRank', () => {
  it('uses the first-rank count, then the next-rank count', () => {
    const knack = talent({ choiceKind: 'skill', choiceCountFirstRank: 1, choiceCountNextRank: 2 })
    expect(choiceCountForNextRank(knack, 0)).toBe(1)
    expect(choiceCountForNextRank(knack, 1)).toBe(2)
    expect(choiceCountForNextRank(talent({}), 0)).toBe(0)
  })
})

describe('talentChoiceOptions', () => {
  it('drops characteristics already chosen by a distinct-across-ranks talent, ignoring case', () => {
    const heroicWill = talent({ choiceKind: 'characteristic', choiceCountFirstRank: 2, choiceDistinctAcrossRanks: true })
    const options = talentChoiceOptions(heroicWill, owned([{ kind: 'characteristic', value: 'Willpower' }]),
      characteristics, [])
    expect(options.map(o => o.value)).toEqual(['brawn', 'agility', 'intellect', 'cunning', 'presence'])
    expect(options.every(o => o.note === undefined)).toBe(true)
  })

  it('caps Dedication at 5 and shows the increase', () => {
    const dedication = talent({
      choiceKind: 'characteristic', choiceCountFirstRank: 1, choiceCountNextRank: 1,
      choiceDistinctAcrossRanks: true, grantsCharacteristic: true,
    })
    const options = talentChoiceOptions(dedication, owned([], ['agility']), characteristics, [])
    expect(options.map(o => o.value)).toEqual(['brawn', 'cunning', 'willpower', 'presence'])
    expect(options.find(o => o.value === 'presence')?.note).toBe('4 → 5')
  })

  it('keeps only allowed skill kinds, new skills and one entry per canonical name', () => {
    const knack = talent({
      choiceKind: 'skill', choiceCountFirstRank: 1, choiceCountNextRank: 2, choiceDistinctAcrossRanks: true,
      choiceAllowedSkillKinds: ['general', 'knowledge', 'social'],
    })
    const skills = [
      skill('Melee', 'combat'), skill('Charm', 'social'), skill('Athletics', 'general'),
      skill('Lore', 'knowledge'), skill('Arcana', 'magic'), skill('Charm', 'social', 'Своё обаяние'),
    ]
    const options = talentChoiceOptions(knack, owned([{ kind: 'skill', value: 'Athletics' }]), characteristics, skills)
    expect(options.map(o => [o.value, o.group])).toEqual([['Lore', 'knowledge'], ['Charm', 'social']])
  })

  it('allows repeating a value when the talent does not require distinct values', () => {
    const natural = talent({ choiceKind: 'skill', choiceCountFirstRank: 2 })
    const options = talentChoiceOptions(natural, owned([{ kind: 'skill', value: 'Melee' }]), characteristics,
      [skill('Melee', 'combat'), skill('Arcana', 'magic')])
    expect(options.map(o => o.value)).toEqual(['Melee', 'Arcana'])
  })
})

describe('signature spell configuration', () => {
  it('sorts effects like the server and keeps multiplicity', () => {
    expect(signatureSpellValue({ action: 'Attack', effects: ['Range', 'Fire', 'Range'] }))
      .toBe('Attack|Fire|Range|Range')
    expect(parseSignatureSpell('Attack|Range|Fire|Range'))
      .toEqual({ action: 'Attack', effects: ['Fire', 'Range', 'Range'] })
  })

  it('rejects a configuration without an action or effects', () => {
    expect(parseSignatureSpell('Attack')).toBeNull()
    expect(parseSignatureSpell('Attack|')).toBeNull()
    expect(parseSignatureSpell('|Fire')).toBeNull()
  })

  it('labels repeated effects with a count', () => {
    expect(signatureSpellLabel({ action: 'Attack', effects: ['Fire', 'Range', 'Range'] }))
      .toBe('Attack: Fire, Range ×2')
  })
})

describe('choiceLabel', () => {
  const choice = (kind: CharacterTalentChoice['kind'], value: string, displayName: string) =>
    ({ rankIndex: 0, kind, value, displayName })

  it('localizes characteristics and skills from the stable value', () => {
    expect(choiceLabel(choice('characteristic', 'Willpower', 'Воля'), [])).toBe('Воля')
    expect(choiceLabel(choice('skill', 'Lore', 'Lore'), [skill('Lore', 'knowledge', 'Предания')])).toBe('Предания')
  })

  it('falls back to the stored snapshot', () => {
    expect(choiceLabel(choice('skill', 'Gone', 'Пропавший навык'), [])).toBe('Пропавший навык')
    expect(choiceLabel(choice('spellConfiguration', 'Attack|Fire', 'Атака: Огонь'), [])).toBe('Атака: Огонь')
    expect(choiceLabel(choice('animalCompanion', 'npc-1', 'Серый сокол'), [])).toBe('Серый сокол')
  })
})
