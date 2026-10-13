import { describe, expect, it } from 'vitest'
import { entityTone, rarityClass, toneClass } from './tones'

describe('v5 visual catalogue', () => {
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
  it('provides safe neutral styling for unknown content', () => {
    expect(toneClass('npc', 'unknown')).toBe('tone-grey')
    expect(toneClass('unknown', 'unknown')).toBe('tone-grey')
  })
})
