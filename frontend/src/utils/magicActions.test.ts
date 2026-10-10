import { describe, expect, it } from 'vitest'
import type { Spell } from '../api/types'
import { magicActions } from './magicActions'

const spell = (id: string, magicSkill: string, nameEn = 'Attack', kind = 'effect') => ({
  id, magicSkill, nameEn, kind,
} as Spell)

describe('magic action definition selection', () => {
  const arcana = spell('arcana', 'Arcana')
  const divine = spell('divine', 'Divine')
  const runes = spell('runes', 'Runes')
  const heal = spell('heal', 'Divine', 'Heal')
  const spells = [arcana, divine, runes, heal, spell('extra', 'Runes', 'Extra', 'additionalEffect')]
  it.each([['Arcana', arcana], ['Divine', divine], ['Runes', runes]])('uses the active %s school for a shared action', (skill, definition) => {
    expect(magicActions(spells, skill as string)).toEqual([definition, heal])
  })
  it('keeps the first definition for unavailable actions and duplicate school entries', () => {
    expect(magicActions([...spells, spell('duplicate', 'Arcana')], 'Arcana')).toEqual([arcana, heal])
    expect(magicActions(spells, 'Primal')).toEqual([arcana, heal])
    expect(magicActions([], 'Arcana')).toEqual([])
  })
})
