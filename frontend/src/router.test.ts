import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { navigate, parseRoute, registerNavigationGuard, usePath, useSearch } from './router'

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

  it('parses idless areas magic, shop, reference, help and about', () => {
    expect(parseRoute('/magic')).toEqual(route({ area: 'magic' }))
    expect(parseRoute('/shop')).toEqual(route({ area: 'shop' }))
    expect(parseRoute('/reference')).toEqual(route({ area: 'reference' }))
    expect(parseRoute('/help')).toEqual(route({ area: 'help' }))
    expect(parseRoute('/about')).toEqual(route({ area: 'about' }))
    expect(parseRoute('/feedback')).toEqual(route({ area: 'feedback' }))
  })

  it('parses public share links with a token', () => {
    expect(parseRoute('/share/raw-token')).toEqual(route({ area: 'share', id: 'raw-token' }))
    expect(parseRoute('/share')).toEqual(route({ area: 'share', unknown: true }))
    expect(parseRoute('/share/raw-token/extra')).toEqual(route({ area: 'share', id: 'raw-token', unknown: true }))
  })

  it('flags trailing segments on idless areas as unknown', () => {
    expect(parseRoute('/about/extra')).toEqual(route({ area: 'about', unknown: true }))
    expect(parseRoute('/help/extra')).toEqual(route({ area: 'help', unknown: true }))
    expect(parseRoute('/magic/extra')).toEqual(route({ area: 'magic', unknown: true }))
    expect(parseRoute('/shop/extra')).toEqual(route({ area: 'shop', unknown: true }))
    expect(parseRoute('/reference/extra')).toEqual(route({ area: 'reference', unknown: true }))
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
    expect(parseRoute('/campaigns/c1/chronicle')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'chronicle' }))
    expect(parseRoute('/campaigns/c1/table')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'table' }))
    // Вкладка «Материалы» (handbook) удалена: старые ссылки — неизвестный маршрут.
    expect(parseRoute('/campaigns/c1/handbook')).toMatchObject({ area: 'campaigns', id: 'c1', sub: null, unknown: true })
    expect(parseRoute('/campaigns/c1/encounters')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'encounters' }))
    expect(parseRoute('/campaigns/c1/custom')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'custom' }))
    expect(parseRoute('/campaigns/c1/settings')).toEqual(route({ area: 'campaigns', id: 'c1', sub: 'settings' }))
  })

  it('parses a specific encounter deep link', () => {
    expect(parseRoute('/campaigns/c1/encounters/e9'))
      .toEqual(route({ area: 'campaigns', id: 'c1', sub: 'encounters', subId: 'e9' }))
  })

  it('parses a campaign member sheet deep link', () => {
    expect(parseRoute('/campaigns/c1/characters/ch1'))
      .toEqual(route({ area: 'campaigns', id: 'c1', sub: 'characters', subId: 'ch1' }))
    expect(parseRoute('/campaigns/c1/characters'))
      .toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
  })

  it('flags sub-views that do not belong to the area as unknown', () => {
    expect(parseRoute('/characters/abc/table')).toEqual(route({ id: 'abc', unknown: true }))
    expect(parseRoute('/campaigns/c1/print')).toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
    expect(parseRoute('/npcs/n1/anything')).toEqual(route({ area: 'npcs', id: 'n1', unknown: true }))
  })

  it('flags an id on a non-encounter campaign sub-view as unknown', () => {
    expect(parseRoute('/campaigns/c1/table/x')).toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
    expect(parseRoute('/campaigns/c1/settings/x')).toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
  })

  it('flags too-deep encounter paths as unknown', () => {
    expect(parseRoute('/campaigns/c1/encounters/e9/extra'))
      .toEqual(route({ area: 'campaigns', id: 'c1', unknown: true }))
  })
})


it('recognizes the personal library and rejects nested library routes', () => {
  expect(parseRoute('/library')).toMatchObject({ area: 'library', unknown: false })
  expect(parseRoute('/library/foreign')).toMatchObject({ area: 'library', unknown: true })
})

it.each(['privacy', 'terms'])('распознаёт публичный /%s и отклоняет лишние сегменты', area => {
  expect(parseRoute(`/${area}`)).toMatchObject({ area, unknown: false })
  expect(parseRoute(`/${area}/extra`).unknown).toBe(true)
})

it('recognizes dedicated NPC creation and edit routes and rejects extra segments', () => {
  expect(parseRoute('/npcs/new')).toMatchObject({ area: 'npcs', id: null, sub: 'new', unknown: false })
  expect(parseRoute('/npcs/n1/edit')).toMatchObject({ area: 'npcs', id: 'n1', sub: 'edit', unknown: false })
  expect(parseRoute('/npcs/new/edit')).toMatchObject({ unknown: true })
  expect(parseRoute('/npcs/n1/edit/extra')).toMatchObject({ unknown: true })
})

it('updates query-only navigation and protects editor routes on navigation and browser Back', () => {
  window.history.replaceState(null, '', '/npcs/n1/edit')
  const hook = renderHook(() => ({ path: usePath(), search: useSearch() }))
  const guard = vi.fn(() => false)
  const push = vi.spyOn(window.history, 'pushState')
  const release = registerNavigationGuard(guard)
  try {
    act(() => navigate('/characters'))
    expect(window.location.pathname).toBe('/npcs/n1/edit')
    act(() => {
      window.history.replaceState(null, '', '/npcs?source=mine')
      window.dispatchEvent(new PopStateEvent('popstate'))
    })
    expect(hook.result.current.path).toBe('/npcs/n1/edit')
    expect(window.location.pathname).toBe('/npcs/n1/edit')
    expect(guard).toHaveBeenCalledWith('/npcs?source=mine')
    expect(push).not.toHaveBeenCalled()
  } finally { release(); push.mockRestore() }
  act(() => navigate('/npcs?source=mine'))
  act(() => navigate('/npcs?source=builtin', { replace: true }))
  expect(hook.result.current).toEqual({ path: '/npcs', search: '?source=builtin' })
  hook.unmount()
})

it('restores rejected Back and Forward without duplicating history or notifying routes', async () => {
  act(() => navigate('/npcs?source=mine'))
  act(() => navigate('/npcs/n1/edit?source=mine'))
  act(() => navigate('/characters'))
  await act(async () => { window.history.back() })
  await waitFor(() => expect(window.location.pathname).toBe('/npcs/n1/edit'))
  const hook = renderHook(() => ({ path: usePath(), search: useSearch() }))
  const guard = vi.fn(() => false)
  const release = registerNavigationGuard(guard)
  const length = window.history.length
  const go = vi.spyOn(window.history, 'go')
  try {
    for (let i = 0; i < 2; i++) {
      await act(async () => { window.history.back() })
      await waitFor(() => expect(guard).toHaveBeenCalledTimes(i + 1))
      await waitFor(() => expect(window.location.pathname).toBe('/npcs/n1/edit'))
      expect(go).toHaveBeenLastCalledWith(1)
      expect(hook.result.current).toEqual({ path: '/npcs/n1/edit', search: '?source=mine' })
      expect(window.history.length).toBe(length)
    }
    await act(async () => { window.history.forward() })
    await waitFor(() => expect(guard).toHaveBeenCalledWith('/characters'))
    await waitFor(() => expect(window.location.pathname).toBe('/npcs/n1/edit'))
    expect(go).toHaveBeenLastCalledWith(-1)
    expect(hook.result.current.path).toBe('/npcs/n1/edit')
    expect(window.history.length).toBe(length)
  } finally { release(); go.mockRestore() }
  await act(async () => { window.history.forward() })
  await waitFor(() => expect(hook.result.current.path).toBe('/characters'))
  hook.unmount()
})

it('keeps navigation working when another popstate interrupts a pending restoration', () => {
  navigate('/campaigns')
  const first = window.history.state
  navigate('/npcs')
  const second = window.history.state
  navigate('/npcs/n1/edit')
  const hook = renderHook(usePath)
  const guard = vi.fn().mockReturnValueOnce(false).mockReturnValue(true)
  const release = registerNavigationGuard(guard)
  // Hold the restoration pending while a second Back event arrives.
  const go = vi.spyOn(window.history, 'go').mockImplementation(() => {})
  try {
    act(() => {
      window.history.replaceState(second, '', '/npcs')
      window.dispatchEvent(new PopStateEvent('popstate', { state: second }))
    })
    expect(go).toHaveBeenCalledWith(1)
    expect(hook.result.current).toBe('/npcs/n1/edit')
    act(() => {
      window.history.replaceState(first, '', '/campaigns')
      window.dispatchEvent(new PopStateEvent('popstate', { state: first }))
    })
    expect(guard).toHaveBeenLastCalledWith('/campaigns')
    expect(hook.result.current).toBe('/campaigns')
    act(() => navigate('/help'))
    expect(hook.result.current).toBe('/help')
    expect(window.location.pathname).toBe('/help')
  } finally { release(); go.mockRestore(); hook.unmount() }
})
