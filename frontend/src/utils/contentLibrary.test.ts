import { describe, expect, it } from 'vitest'
import { contentKey, filterPickerRows, toggleSelection, vitalState } from './contentLibrary'

describe('content picker selection', () => {
  const rows = [{ key: 'skill:a', group: 'skill', groupLabel: 'Skills', name: 'Sailing', editedAt: '2026-10-09' },
    { key: 'skill:b', group: 'skill', groupLabel: 'Skills', name: 'Cooking', editedAt: '2026-10-08' },
    { key: 'talent:a', group: 'talent', groupLabel: 'Talents', name: 'Captain', editedAt: null }]
  it('keeps keys from other groups when selecting filtered visible rows', () => {
    const visible = filterPickerRows(rows, 'skill', 'sail', 'name')
    expect(toggleSelection(['talent:a'], visible.map(x => x.key), true)).toEqual(['talent:a', 'skill:a'])
    expect(toggleSelection(['talent:a', 'skill:a'], visible.map(x => x.key), false)).toEqual(['talent:a'])
    expect(contentKey('skill', 'a')).not.toBe(contentKey('talent', 'a'))
  })
  it('sorts without mutating the source and uses names as deterministic tie breakers', () => {
    expect(filterPickerRows(rows, 'all', '', 'recent').map(x => x.name)).toEqual(['Sailing', 'Cooking', 'Captain'])
    expect(rows[0].name).toBe('Sailing')
    expect(filterPickerRows(rows, 'skill', '', 'name').map(x => x.name)).toEqual(['Cooking', 'Sailing'])
  })
})
describe('sheet vitals', () => {
  it('handles every threshold boundary and rounds the near interval upwards', () => {
    expect([7, 8, 11, 12].map(n => vitalState(n, 11))).toEqual(['normal', 'near', 'threshold', 'over'])
    expect(vitalState(0, 0)).toBe('threshold')
  })

})
