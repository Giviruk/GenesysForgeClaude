import { describe, expect, it } from 'vitest'
import type { NpcRole } from '../api/types'
import { activationKey, entityTone, rarityClass, toneClass } from './tones'

describe('v5 visual catalogue', () => {
  const roles: Record<NpcRole, string> = {
    brute: 'red', skirmisher: 'green', archer: 'teal', caster: 'purple', leader: 'orange',
    social: 'navy', support: 'red', monster: 'grey', custom: 'grey',
  }
  it.each(Object.entries(roles))('maps the real NPC role %s to %s', (role, tone) => {
    expect(entityTone('role', role)).toBe(tone)
    expect(toneClass('role', role)).toBe(`tone-${tone}`)
  })
  it('keeps the approved rival exception limited to encounters and the table', () => {
    expect(entityTone('npc', 'rival')).toBe('blue')
    expect(entityTone('participant', 'rival')).toBe('orange')
  })
  it('uses one rarity scale at every boundary', () => {
    expect([0, 2, 3, 4, 5, 6, 10].map(rarityClass)).toEqual([
      'rarity-common', 'rarity-common', 'rarity-uncommon', 'rarity-rare', 'rarity-rare', 'rarity-exotic', 'rarity-exotic',
    ])
  })
  it('colours activation metadata with timing and ranked qualifiers', () => {
    expect(entityTone('activation', 'Out-of-turn Incidental')).toBe('orange')
    expect(entityTone('activation', 'Maneuver (Ranked)')).toBe('green')
    expect(entityTone('activation', 'Passive (Ranked)')).toBe('grey')
  })
  it('normalizes bilingual activation metadata before looking up its colour', () => {
    const cases = [
      [' Passive (Ranked) ', 'passive', 'grey'], ['Пассивный', 'passive', 'grey'],
      ['Out-of-turn Incidental', 'incidental', 'orange'], ['Свободное действие', 'incidental', 'orange'],
      ['Manoeuvre', 'maneuver', 'green'], ['Манёвр', 'maneuver', 'green'],
      ['Action', 'action', 'blue'], ['Действие', 'action', 'blue'],
      ['Unknown', null, 'grey'],
    ] as const
    for (const [value, key, tone] of cases) {
      expect(activationKey(value)).toBe(key)
      expect(entityTone('activation', value)).toBe(tone)
    }
  })
  it('provides safe neutral styling for unknown content', () => {
    expect(toneClass('npc', 'unknown')).toBe('tone-grey')
    expect(toneClass('unknown', 'unknown')).toBe('tone-grey')
  })
})
