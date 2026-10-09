// Собственная аналитика загружается только после отдельного согласия.
type AriadneProperties = Record<string, string | number | boolean | null>
export type AnalyticsConsent = 'granted' | 'denied'
export const ANALYTICS_CONSENT_KEY = 'genesysforge.analytics-consent'
export const ANALYTICS_CONSENT_CHANGED = 'genesysforge:analytics-consent'
export const ANALYTICS_PREFERENCES = 'genesysforge:analytics-preferences'

interface AriadneBrowserClient {
  track(name: string, properties?: AriadneProperties): AriadneBrowserClient
  getAnonymousId(): string
  reset(): AriadneBrowserClient
  optOut(value?: boolean): AriadneBrowserClient
}

declare global {
  interface Window {
    ariadne?: AriadneBrowserClient
    __ariadneAnonymousId?: string
  }
}

export function analyticsConfigured(): boolean {
  return !!(import.meta.env.VITE_ARIADNE_ENDPOINT && import.meta.env.VITE_ARIADNE_PROJECT_KEY)
}

export function readAnalyticsConsent(): AnalyticsConsent | null {
  try {
    const value = localStorage.getItem(ANALYTICS_CONSENT_KEY)
    return value === 'granted' || value === 'denied' ? value : null
  } catch {
    return null
  }
}

export function initAriadne(): void {
  const endpoint = import.meta.env.VITE_ARIADNE_ENDPOINT?.replace(/\/$/, '')
  const projectKey = import.meta.env.VITE_ARIADNE_PROJECT_KEY
  if (readAnalyticsConsent() !== 'granted' || !endpoint || !projectKey) return
  if (document.querySelector('script[data-ariadne]')) return
  try {
    const script = document.createElement('script')
    script.defer = true
    script.src = `${endpoint}/tracker.js`
    script.dataset.ariadne = 'true'
    script.dataset.endpoint = endpoint
    script.dataset.projectKey = projectKey
    document.head.appendChild(script)
  } catch {
    // Сбой аналитики не мешает работе сервиса.
  }
}

// Ключи реального Ariadne SDK. reload останавливает таймеры, очередь и обработчики SDK.
export function disableAriadne(reload: () => void = () => window.location.reload(), forceReload = false): void {
  const running = !!window.ariadne || !!document.querySelector('script[data-ariadne]')
  try { window.ariadne?.optOut(true) } catch { /* SDK может быть недоступен. */ }
  try { window.ariadne?.reset() } catch { /* Удаляем ключи также напрямую. */ }
  try {
    localStorage.removeItem('ariadne.anonymous')
    localStorage.setItem('ariadne.optout', '1')
    sessionStorage.removeItem('ariadne.session')
    sessionStorage.removeItem('ariadne.activity')
  } catch { /* Недоступное хранилище не разрешает сбор. */ }
  delete window.__ariadneAnonymousId
  document.querySelector('script[data-ariadne]')?.remove()
  if (running || forceReload) reload()
}

export function setAnalyticsConsent(consent: AnalyticsConsent): boolean {
  const previous = readAnalyticsConsent()
  try { localStorage.setItem(ANALYTICS_CONSENT_KEY, consent) } catch {
    if (consent === 'granted') return false
  }
  if (consent === 'granted') {
    try { localStorage.removeItem('ariadne.optout'); window.ariadne?.optOut(false) } catch { /* noop */ }
    initAriadne()
  } else {
    // Даже при не завершившейся загрузке трекера отзыв прекращает его выполнение.
    disableAriadne(undefined, previous === 'granted')
  }
  window.dispatchEvent(new Event(ANALYTICS_CONSENT_CHANGED))
  return true
}

export function openAnalyticsPreferences(): void {
  window.dispatchEvent(new Event(ANALYTICS_PREFERENCES))
}

export function trackAriadne(name: string, properties?: AriadneProperties): void {
  if (readAnalyticsConsent() !== 'granted') return
  try { window.ariadne?.track(name, properties) } catch { /* noop */ }
}

/** Только согласившийся браузер передаёт ID для события регистрации. */
export function ariadneAnonymousId(): string | undefined {
  if (readAnalyticsConsent() !== 'granted') return undefined
  try { return window.ariadne?.getAnonymousId() } catch { return undefined }
}
