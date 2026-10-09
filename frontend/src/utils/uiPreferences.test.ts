import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { readRangeTrackerState, readSheetTab, writeRangeTrackerState, writeSheetTab, readSkillProgress, writeSkillProgress } from './uiPreferences'

describe('UI preferences persistence', () => {
  beforeEach(() => {
    for (const key of Object.keys(localStorage)) {
      if (key.startsWith('genesysforge.skillProgress.') || key.startsWith('genesysforge.sheet-tab.') || key.startsWith('genesysforge.game-table.range.')) {
        localStorage.removeItem(key)
      }
    }
  })

  afterEach(() => vi.restoreAllMocks())

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
    writeSheetTab('c2', 'notes')

    expect(readSheetTab('c1')).toBe('inventory')
    expect(readSheetTab('c2')).toBe('notes')
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
