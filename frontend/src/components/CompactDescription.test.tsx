import { act, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import { CompactDescription } from './CompactDescription'

afterEach(() => vi.unstubAllGlobals())

it('shows a source link without an unnecessary disclosure for a short description', () => {
  render(<CompactDescription source="Realms of Terrinoth, с. 1">Короткий собственный текст.</CompactDescription>)
  expect(screen.getByRole('link').textContent).toContain('Realms of Terrinoth')
  expect(screen.queryByRole('button', { name: /Подробнее/ })).toBeNull()
})

it('offers disclosure only while the measured text is clipped, including after a resize', () => {
  let notify!: () => void
  vi.stubGlobal('ResizeObserver', class {
    constructor(callback: ResizeObserverCallback) { notify = () => callback([], this as unknown as ResizeObserver) }
    observe() {}
    disconnect() {}
  })
  const { container } = render(<CompactDescription>Собственный текст для проверки обрезки.</CompactDescription>)
  const text = container.querySelector('.compact-description-text')!
  Object.defineProperty(text, 'clientHeight', { configurable: true, value: 40 })
  Object.defineProperty(text, 'scrollHeight', { configurable: true, value: 80 })
  act(notify)
  const toggle = screen.getByRole('button', { name: /Подробнее/ })
  fireEvent.click(toggle)
  expect(toggle.getAttribute('aria-expanded')).toBe('true')
  fireEvent.click(toggle)
  Object.defineProperty(text, 'scrollHeight', { configurable: true, value: 40 })
  act(notify)
  expect(screen.queryByRole('button', { name: /Подробнее/ })).toBeNull()
})
