import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AnalyticsConsent } from './AnalyticsConsent'
import { Footer } from './Footer'
import { ANALYTICS_CONSENT_KEY } from '../analytics/ariadne'

beforeEach(() => {
  localStorage.removeItem(ANALYTICS_CONSENT_KEY)
  delete window.ariadne
  document.querySelectorAll('script[data-ariadne]').forEach(script => script.remove())
  vi.stubEnv('VITE_ARIADNE_ENDPOINT', 'https://analytics.test')
  vi.stubEnv('VITE_ARIADNE_PROJECT_KEY', 'pub_test')
})
afterEach(() => { cleanup(); vi.unstubAllEnvs(); localStorage.removeItem(ANALYTICS_CONSENT_KEY) })

describe('баннер аналитики', () => {
  it('даёт равные кнопки, ссылку и сохраняет отказ без загрузки', () => {
    render(<><AnalyticsConsent /><Footer /></>)
    const banner = screen.getByRole('region', { name: 'Согласие на аналитику' })
    const accept = screen.getByRole('button', { name: 'Принять' })
    const decline = screen.getByRole('button', { name: 'Отказаться' })
    expect(accept.className).toBe(decline.className)
    expect(banner.querySelector('a')?.getAttribute('href')).toBe('/privacy')
    fireEvent.click(decline)
    expect(localStorage.getItem(ANALYTICS_CONSENT_KEY)).toBe('denied')
    expect(screen.queryByRole('region')).toBeNull()
    expect(document.querySelector('script[data-ariadne]')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Аналитика' }))
    expect(screen.getByRole('region')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Принять' }))
    expect(localStorage.getItem(ANALYTICS_CONSENT_KEY)).toBe('granted')
    expect(document.querySelector('script[data-ariadne]')).toBeTruthy()
    expect(screen.queryByRole('region')).toBeNull()
  })
  it('не показывает баннер повторно при сохранённом отказе', () => {
    localStorage.setItem(ANALYTICS_CONSENT_KEY, 'denied')
    render(<AnalyticsConsent />)
    expect(screen.queryByRole('region')).toBeNull()
  })
})
