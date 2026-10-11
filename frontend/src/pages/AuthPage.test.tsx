import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { expect, it, vi } from 'vitest'
import { AuthPage } from './AuthPage'
import { navigate } from '../router'

const confirmReset = vi.hoisted(() => vi.fn().mockResolvedValue(undefined))
vi.mock('../api/client', () => ({ api: {
  authProviders: vi.fn().mockResolvedValue({ googleClientId: null }),
  confirmPasswordReset: confirmReset,
} }))
vi.mock('../auth-context', () => ({ useAuth: () => ({ sessionExpired: false }) }))
vi.mock('../analytics/ariadne', () => ({ trackAriadne: vi.fn(), analyticsConfigured: () => false, openAnalyticsPreferences: vi.fn() }))

it('removes the reset token while preserving history data and returning to sign in', async () => {
  navigate('/login?token=test-reset-token', { replace: true })
  const historyState = { ...window.history.state, scroll: 120 }
  window.history.replaceState(historyState, '', window.location.href)
  const length = window.history.length
  render(<AuthPage />)
  fireEvent.change(screen.getByLabelText('Новый пароль'), { target: { value: 'New-Password-123!' } })
  fireEvent.click(screen.getByRole('button', { name: 'Сохранить пароль' }))
  await waitFor(() => expect(screen.getByText('Пароль обновлён — войдите с новым паролем.')).toBeTruthy())
  expect(confirmReset).toHaveBeenCalledWith('test-reset-token', 'New-Password-123!')
  expect(window.location.pathname).toBe('/login')
  expect(window.location.search).toBe('')
  expect(window.history.state).toEqual(historyState)
  expect(window.history.length).toBe(length)
  expect(screen.getByRole('button', { name: 'Войти' })).toBeTruthy()
})
