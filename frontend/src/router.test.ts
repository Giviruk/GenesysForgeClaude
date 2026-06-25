import { describe, expect, it } from 'vitest'
import { parseRoute } from './router'

const route = (over: Partial<ReturnType<typeof parseRoute>> = {}) =>
  ({ area: 'characters', id: null, sub: null, subId: null, unknown: false, ...over })

describe('parseRoute', () => {
  it('defaults the root path to the characters area', () => {
    expect(parseRoute('/')).toEqual(route())
  })

  it('treats login/register as the characters area (auth handled separately)', () => {
    expect(parseRoute('/login')).toEqual(route())
    expect(parseRoute('/register')).toEqual(route())
  })

  it('parses entity areas with and without an id', () => {
    expect(parseRoute('/characters')).toEqual(route())
    expect(parseRoute('/characters/abc')).toEqual(route({ id: 'abc' }))
    expect(parseRoute('/campaigns/xyz')).toEqual(route({ area: 'campaigns', id: 'xyz' }))
    expect(parseRoute('/npcs/n1')).toEqual(route({ area: 'npcs', id: 'n1' }))
  })

  it('parses idless areas magic and about', () => {
    expect(parseRoute('/magic')).toEqual(route({ area: 'magic' }))
    expect(parseRoute('/about')).toEqual(route({ area: 'about' }))
  })

  it('flags trailing segments on idless areas as unknown', () => {
    expect(parseRoute('/about/extra')).toEqual(route({ area: 'about', unknown: true }))
    expect(parseRoute('/magic/extra')).toEqual(route({ area: 'magic', unknown: true }))
  })

  it('flags unknown heads as unknown', () => {
    expect(parseRoute('/nope')).toEqual(route({ unknown: true }))
  })

  it('ignores surrounding slashes', () => {
    expect(parseRoute('/about/')).toEqual(route({ area: 'about' }))
  })

  it('parses the printable character sub-route', () => {
    expect(parseRoute('/characters/abc/print')).toEqual(route({ id: 'abc', sub: 'print' }))
  })

  it('parses campaign sub-views', () => {
    expect(parseRoute('/campaigns/c1/table')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'table' }))
    expect(parseRoute('/campaigns/c1/handbook')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'handbook' }))
    expect(parseRoute('/campaigns/c1/encounters')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'encounters' }))
  })

  it('parses a specific encounter deep link', () => {
    expect(parseRoute('/campaigns/c1/encounters/e9'))
      .toEqual(route({ area: 'campaigns', id: 'c1', sub: 'encounters', subId: 'e9' }))
  })

  it('flags sub-views that do not belong to the area as unknown', () => {
    expect(parseRoute('/characters/abc/table')).toEqual(route({ id: 'abc', unknown: true }))
    expect(parseRoute('/campaigns/c1/print')).toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
    expect(parseRoute('/npcs/n1/anything')).toEqual(route({ area: 'npcs', id: 'n1', unknown: true }))
  })

  it('flags an id on a non-encounter campaign sub-view as unknown', () => {
    expect(parseRoute('/campaigns/c1/table/x')).toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
  })

  it('flags too-deep encounter paths as unknown', () => {
    expect(parseRoute('/campaigns/c1/encounters/e9/extra'))
      .toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
  })
})
