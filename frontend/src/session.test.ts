import { afterEach, describe, expect, it, vi } from 'vitest'
import { captureReturnTo, clearReturnTo, consumeReturnTo, peekReturnTo, restoreReturnTo } from './session'

describe('session — точка возврата после истёкшей сессии', () => {
  afterEach(() => sessionStorage.clear())

  it('сохраняет осмысленный путь и читает его', () => {
    captureReturnTo('/characters/abc')
    expect(peekReturnTo()).toBe('/characters/abc')
  })

  it('не сохраняет корень и экраны авторизации', () => {
    captureReturnTo('/')
    captureReturnTo('/login')
    captureReturnTo('/register')
    expect(peekReturnTo()).toBeNull()
  })

  it('peek не очищает, consume очищает', () => {
    captureReturnTo('/campaigns/42')
    expect(peekReturnTo()).toBe('/campaigns/42')
    expect(peekReturnTo()).toBe('/campaigns/42') // всё ещё на месте
    expect(consumeReturnTo()).toBe('/campaigns/42')
    expect(peekReturnTo()).toBeNull() // после consume пусто
  })

  it('clearReturnTo забывает сохранённый путь', () => {
    captureReturnTo('/npcs/7')
    clearReturnTo()
    expect(consumeReturnTo()).toBeNull()
  })

  it('restores the saved page without changing history data or adding an entry', () => {
    const state = { __genesysForgeIndex: 7, scroll: 180 }
    window.history.replaceState(state, '', '/login')
    const length = window.history.length
    const pop = vi.fn()
    window.addEventListener('popstate', pop)
    try {
      captureReturnTo('/campaigns/c/content')
      restoreReturnTo()
      expect(window.location.pathname).toBe('/campaigns/c/content')
      expect(window.history.state).toEqual(state)
      expect(window.history.length).toBe(length)
      expect(pop).toHaveBeenCalledTimes(1)
      expect(peekReturnTo()).toBeNull()
    } finally { window.removeEventListener('popstate', pop) }
  })
})
