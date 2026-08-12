import { beforeEach, describe, expect, it } from 'vitest'
import { readRangeTrackerState, readSheetTab, writeRangeTrackerState, writeSheetTab } from './uiPreferences'

describe('UI preferences persistence', () => {
  beforeEach(() => {
    for (const key of Object.keys(localStorage)) {
      if (key.startsWith('genesysforge.sheet-tab.') || key.startsWith('genesysforge.game-table.range.')) {
        localStorage.removeItem(key)
      }
    }
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
