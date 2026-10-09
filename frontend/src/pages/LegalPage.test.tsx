import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import App from '../App'
import { api, tokenStorage } from '../api/client'

afterEach(() => { cleanup(); vi.restoreAllMocks(); tokenStorage.clear() })
it.each(['privacy', 'terms'] as const)('публичный /%s открывается без аккаунта', async document => {
  tokenStorage.clear()
  vi.spyOn(api, 'refresh').mockRejectedValue(new Error('no session'))
  window.history.replaceState(null, '', `/${document}`)
  render(<App />)
  const title = document === 'privacy' ? 'Политика обработки персональных данных GenesysForge' : 'Пользовательское соглашение GenesysForge'
  expect(await screen.findByRole('heading', { name: title })).toBeTruthy()
  expect(screen.getByRole('link', { name: 'Соглашение' }).getAttribute('href')).toBe('/terms')
  expect(screen.getByRole('link', { name: 'Политика конфиденциальности' }).getAttribute('href')).toBe('/privacy')
})
