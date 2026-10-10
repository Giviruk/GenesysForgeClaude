import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { readRangeTrackerState, readSheetTab, writeRangeTrackerState, writeSheetTab, readSkillProgress, writeSkillProgress, readWorkshopMode, writeWorkshopMode, readHeroicUses, writeHeroicUses } from './uiPreferences'

describe('UI preferences persistence', () => {
  beforeEach(() => {
    for (const key of Object.keys(localStorage)) {
      if (key.startsWith('genesysforge.')) {
        localStorage.removeItem(key)
      }
    }
  })

  afterEach(() => vi.restoreAllMocks())

  it.each([['attachments', 'workshop', 'upgrades'], ['crafting', 'workshop', 'craft'], ['notes', 'bio', 'upgrades']])(
    'migrates the old %s tab and saves its workshop mode', (oldTab, tab, mode) => {
      localStorage.setItem('genesysforge.sheet-tab.c1', oldTab)
      expect(readSheetTab('c1')).toBe(tab)
      expect(readWorkshopMode('c1')).toBe(mode)
      expect(localStorage.getItem('genesysforge.sheet-tab.c1')).toBe(tab)
      writeWorkshopMode('c1', 'craft')
      expect(readSheetTab('c1')).toBe(tab)
      expect(readWorkshopMode('c1')).toBe('craft')
      expect(readWorkshopMode('c2')).toBe('upgrades')
    },
  )

  it('stores heroic uses per character and recovers from invalid values', () => {
    writeHeroicUses('c1', 2)
    expect(readHeroicUses('c1')).toBe(2)
    expect(readHeroicUses('c2')).toBe(0)
    for (const value of ['-1', '1.5', 'NaN', 'Infinity', 'broken']) {
      localStorage.setItem('genesysforge.heroic-uses.c2', value)
      expect(readHeroicUses('c2')).toBe(0)
    }
    writeHeroicUses('c1', 0)
    expect(readHeroicUses('c1')).toBe(0)
    writeHeroicUses('c1', NaN)
    expect(readHeroicUses('c1')).toBe(0)
  })

  it('keeps workshop and heroic controls usable when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('unavailable') })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('unavailable') })
    expect(readWorkshopMode('c1')).toBe('upgrades')
    expect(readHeroicUses('c1')).toBe(0)
    expect(() => writeWorkshopMode('c1', 'craft')).not.toThrow()
    expect(() => writeHeroicUses('c1', 1)).not.toThrow()
  })

  it('persists skill progression per character and defaults to the creation phase', () => {
    expect(readSkillProgress('new', true)).toBe(true)
    expect(readSkillProgress('new', false)).toBe(false)
    writeSkillProgress('c1', false)
    writeSkillProgress('c2', true)
    expect(localStorage.getItem('genesysforge.skillProgress.c1')).toBe('false')
    expect(readSkillProgress('c1', true)).toBe(false)
    expect(readSkillProgress('c2', false)).toBe(true)
    localStorage.setItem('genesysforge.skillProgress.corrupt', 'broken')
    expect(readSkillProgress('corrupt', true)).toBe(true)
    expect(readSkillProgress('corrupt', false)).toBe(false)
  })

  it('uses the default and ignores writes when storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('unavailable') })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('unavailable') })
    expect(readSkillProgress('c', true)).toBe(true)
    expect(readSkillProgress('c', false)).toBe(false)
    expect(() => writeSkillProgress('c', true)).not.toThrow()
  })

  it('stores the last sheet tab separately for each character', () => {
    writeSheetTab('c1', 'inventory')
    writeSheetTab('c2', 'bio')

    expect(readSheetTab('c1')).toBe('inventory')
    expect(readSheetTab('c2')).toBe('bio')
    expect(readSheetTab('c3')).toBe('sheet')
  })

  it('ignores corrupt sheet tabs and range tracker values', () => {
    localStorage.setItem('genesysforge.sheet-tab.c1', 'unknown')
    localStorage.setItem('genesysforge.game-table.range.c1.s1', JSON.stringify({
      zones: { valid: 'long', invalid: 'somewhere' }, log: ['move', 42],
    }))

    expect(readSheetTab('c1')).toBe('sheet')
    expect(readRangeTrackerState('c1', 's1')).toEqual({
      zones: { valid: 'long' }, log: ['move'], angles: {}, focusParticipantId: null,
    })
  })

  it('stores range tracker state independently for each scene', () => {
    writeRangeTrackerState('c1', 's1', {
      zones: { p1: 'extreme' }, log: ['move'], angles: { p1: 450 }, focusParticipantId: 'p1',
    })

    expect(readRangeTrackerState('c1', 's1')).toEqual({
      zones: { p1: 'extreme' }, log: ['move'], angles: { p1: 90 }, focusParticipantId: 'p1',
    })
    expect(readRangeTrackerState('c1', 's2')).toEqual({
      zones: {}, log: [], angles: {}, focusParticipantId: null,
    })
  })
})
