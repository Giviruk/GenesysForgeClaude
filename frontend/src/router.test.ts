import { describe, expect, it } from 'vitest'
import { parseRoute } from './router'

describe('parseRoute', () => {
  it('defaults the root path to the characters area', () => {
    expect(parseRoute('/')).toEqual({ area: 'characters', id: null, unknown: false })
  })

  it('treats login/register as the characters area (auth handled separately)', () => {
    expect(parseRoute('/login')).toEqual({ area: 'characters', id: null, unknown: false })
    expect(parseRoute('/register')).toEqual({ area: 'characters', id: null, unknown: false })
  })

  it('parses entity areas with and without an id', () => {
    expect(parseRoute('/characters')).toEqual({ area: 'characters', id: null, unknown: false })
    expect(parseRoute('/characters/abc')).toEqual({ area: 'characters', id: 'abc', unknown: false })
    expect(parseRoute('/campaigns/xyz')).toEqual({ area: 'campaigns', id: 'xyz', unknown: false })
    expect(parseRoute('/npcs/n1')).toEqual({ area: 'npcs', id: 'n1', unknown: false })
  })

  it('parses idless areas magic and about', () => {
    expect(parseRoute('/magic')).toEqual({ area: 'magic', id: null, unknown: false })
    expect(parseRoute('/about')).toEqual({ area: 'about', id: null, unknown: false })
  })

  it('flags trailing segments on idless areas as unknown', () => {
    expect(parseRoute('/about/extra')).toEqual({ area: 'about', id: null, unknown: true })
    expect(parseRoute('/magic/extra')).toEqual({ area: 'magic', id: null, unknown: true })
  })

  it('flags too-deep entity paths and unknown heads as unknown', () => {
    expect(parseRoute('/characters/a/b')).toEqual({ area: 'characters', id: 'a', unknown: true })
    expect(parseRoute('/nope')).toEqual({ area: 'characters', id: null, unknown: true })
  })

  it('ignores surrounding slashes', () => {
    expect(parseRoute('/about/')).toEqual({ area: 'about', id: null, unknown: false })
  })
})
