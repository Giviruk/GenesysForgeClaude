import { describe, expect, it } from 'vitest'
import type { NpcListItem, Reference, TalentDef } from '../api/types'
import { bestiaryQuery, EMPTY_FILTERS, filterBestiary, findNpcTalent, readBestiaryFilters, suggestedNpcSoak } from './bestiary'

const items = [
  { id: 'a', name: 'Alpha', system: 'genesysCore', kind: 'minion', role: 'brute', isMine: true, isBuiltIn: false, createdAt: '2026-01-01' },
  { id: 'b', name: 'Beta', system: 'realmsOfTerrinoth', kind: 'rival', role: 'leader', isMine: false, isBuiltIn: true, createdAt: '2026-02-01' },
  { id: 'c', name: 'Gamma', system: 'realmsOfTerrinoth', kind: 'nemesis', role: 'leader', isMine: true, isBuiltIn: false, createdAt: '2026-03-01' },
] as NpcListItem[]
describe('Bestiary filters', () => {
  it('round trips all filter fields, multiple kinds and Cyrillic search through the URL', () => {
    const f = { ...EMPTY_FILTERS, search: 'Лесной страж', kinds: ['rival', 'nemesis'] as const, system: 'realmsOfTerrinoth' as const, role: 'leader' as const, source: 'mine' as const, sort: 'name' as const }
    expect(readBestiaryFilters(bestiaryQuery({ ...f, kinds: [...f.kinds] }))).toEqual(f)
  })
  it('rejects unsupported query values', () => {
    expect(readBestiaryFilters('?kind=unknown&role=unknown&system=unknown&source=unknown&sort=unknown')).toEqual(EMPTY_FILTERS)
  })
  it('combines system, role, source, multiselect kinds, search and sorting without mutating input', () => {
    expect(filterBestiary(items, { ...EMPTY_FILTERS, kinds: ['minion', 'nemesis'] }).map(n => n.id)).toEqual(['c', 'a'])
    expect(filterBestiary(items, { ...EMPTY_FILTERS, system: 'realmsOfTerrinoth', role: 'leader', source: 'mine', search: ' GAM ', sort: 'name' }).map(n => n.id)).toEqual(['c'])
    expect(items.map(n => n.id)).toEqual(['a', 'b', 'c'])
    expect(filterBestiary(items, { ...EMPTY_FILTERS, source: 'builtin' }).map(n => n.id)).toEqual(['b'])
  })
})
describe('editor reference lookup', () => {
  it('suggests soak from Brawn and catalog armor, matching both saved languages', () => {
    const ref = { items: [{ kind: 'armor', name: 'Mail', nameRu: 'Кольчуга', soakBonus: 2 }] } as Reference
    expect(suggestedNpcSoak({ brawn: 3, equipment: ['Кольчуга'] }, ref)).toBe(5)
    expect(suggestedNpcSoak({ brawn: 4, equipment: ['Mail'] }, ref)).toBe(6)
    expect(suggestedNpcSoak({ brawn: 4, equipment: [] }, ref)).toBe(4)
  })
  it('finds talents in either language with an optional NPC rank and leaves custom names unknown', () => {
    const talent = { name: 'Parry', nameRu: 'Парирование' } as TalentDef
    expect(findNpcTalent('Парирование 2', [talent])).toBe(talent)
    expect(findNpcTalent('Parry 1', [talent])).toBe(talent)
    expect(findNpcTalent('Not a reference talent', [talent])).toBeUndefined()
  })
})
