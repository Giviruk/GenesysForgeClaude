import type { KeyboardEvent } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { activateLinkOnKey } from './linkActivation'

describe('keyboard activation of card links', () => {
  function event(key: string, nested = false) {
    const card = document.createElement('div')
    return { key, currentTarget: card, target: nested ? document.createElement('button') : card,
      preventDefault: vi.fn() } as unknown as KeyboardEvent<HTMLElement>
  }
  it.each(['Enter', ' '])('activates %s on the card and prevents default navigation/scroll', key => {
    const e = event(key)
    const activate = vi.fn()
    activateLinkOnKey(e, activate)
    expect(activate).toHaveBeenCalledOnce()
    expect(e.preventDefault).toHaveBeenCalledOnce()
  })
  it('leaves nested management buttons in control of their own keyboard action', () => {
    const e = event('Enter', true)
    const activate = vi.fn()
    activateLinkOnKey(e, activate)
    expect(activate).not.toHaveBeenCalled()
    expect(e.preventDefault).not.toHaveBeenCalled()
  })
  it('ignores unrelated keys on the card', () => {
    const e = event('Escape')
    const activate = vi.fn()
    activateLinkOnKey(e, activate)
    expect(activate).not.toHaveBeenCalled()
    expect(e.preventDefault).not.toHaveBeenCalled()
  })
})
