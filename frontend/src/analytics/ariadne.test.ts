import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ANALYTICS_CONSENT_KEY, ariadneAnonymousId, disableAriadne, initAriadne, readAnalyticsConsent, setAnalyticsConsent, trackAriadne } from './ariadne'
import { api } from '../api/client'

beforeEach(() => {
  localStorage.clear(); sessionStorage.clear()
  vi.stubEnv('VITE_ARIADNE_ENDPOINT', 'https://analytics.test')
  vi.stubEnv('VITE_ARIADNE_PROJECT_KEY', 'pub_test')
  document.querySelectorAll('script[data-ariadne]').forEach(script => script.remove())
  delete window.ariadne
})
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllEnvs(); delete window.ariadne; localStorage.clear() })

function tracker() {
  const client = { track: vi.fn(), getAnonymousId: vi.fn(() => '11111111-1111-1111-1111-111111111111'), reset: vi.fn(), optOut: vi.fn() }
  window.ariadne = client as unknown as NonNullable<Window['ariadne']>
  return client
}

describe('отдельное согласие на Ариадну', () => {
  it.each([null, 'denied', 'unexpected'])('без разрешения (%s) нет загрузки, события и заголовка регистрации', async choice => {
    if (choice) localStorage.setItem(ANALYTICS_CONSENT_KEY, choice)
    const client = tracker()
    initAriadne(); trackAriadne('test_event')
    const request = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('{}', { status: 200 }))
    await api.register('user@test.local', 'password123', 'Tester')
    expect(document.querySelector('script[data-ariadne]')).toBeNull()
    expect(ariadneAnonymousId()).toBeUndefined()
    expect(client.track).not.toHaveBeenCalled()
    expect(client.getAnonymousId).not.toHaveBeenCalled()
    expect(new Headers(request.mock.calls[0][1]?.headers).has('X-Ariadne-Anonymous-Id')).toBe(false)
  })

  it('после разрешения загружает один tracker и передаёт анонимный ID', async () => {
    expect(setAnalyticsConsent('granted')).toBe(true)
    initAriadne()
    expect(readAnalyticsConsent()).toBe('granted')
    expect(document.querySelectorAll('script[data-ariadne]')).toHaveLength(1)
    expect(document.querySelector('script[data-ariadne]')?.getAttribute('src')).toBe('https://analytics.test/tracker.js')
    const client = tracker()
    const request = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('{}', { status: 200 }))
    await api.register('user@test.local', 'password123', 'Tester')
    expect(new Headers(request.mock.calls[0][1]?.headers).get('X-Ariadne-Anonymous-Id')).toBe(client.getAnonymousId())
  })

  it('при отзыве очищает реальные ключи SDK, останавливает его и перезагружает страницу', () => {
    const client = tracker()
    localStorage.setItem(ANALYTICS_CONSENT_KEY, 'denied')
    localStorage.setItem('ariadne.anonymous', 'old-id')
    sessionStorage.setItem('ariadne.session', 'session')
    sessionStorage.setItem('ariadne.activity', 'time')
    window.__ariadneAnonymousId = 'fallback-id'
    const reload = vi.fn()
    disableAriadne(reload)
    expect(localStorage.getItem('ariadne.anonymous')).toBeNull()
    expect(sessionStorage.getItem('ariadne.session')).toBeNull()
    expect(sessionStorage.getItem('ariadne.activity')).toBeNull()
    expect(window.__ariadneAnonymousId).toBeUndefined()
    expect(localStorage.getItem('ariadne.optout')).toBe('1')
    expect(client.optOut).toHaveBeenCalledWith(true)
    expect(client.reset).toHaveBeenCalledOnce()
    expect(reload).toHaveBeenCalledOnce()
    expect(ariadneAnonymousId()).toBeUndefined()
  })

  it('при недоступном localStorage не запускает сбор', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('blocked') })
    expect(setAnalyticsConsent('granted')).toBe(false)
    expect(document.querySelector('script[data-ariadne]')).toBeNull()
  })
})
