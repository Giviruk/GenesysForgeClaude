import { afterEach, describe, expect, it } from 'vitest'
import { captureReturnTo, clearReturnTo, consumeReturnTo, peekReturnTo } from './session'

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
})
