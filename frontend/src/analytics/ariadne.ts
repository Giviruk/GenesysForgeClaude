// Подключение «Ариадны» (self-hosted аналитика пути пользователя).
// Без VITE_ARIADNE_ENDPOINT и VITE_ARIADNE_PROJECT_KEY модуль не грузит tracker и молчит,
// поэтому локальная разработка и тесты остаются без внешних запросов.

type AriadneProperties = Record<string, string | number | boolean | null>

interface AriadneBrowserClient {
  track(name: string, properties?: AriadneProperties): AriadneBrowserClient
  getAnonymousId(): string
}

declare global {
  interface Window {
    ariadne?: AriadneBrowserClient
  }
}

const endpoint = import.meta.env.VITE_ARIADNE_ENDPOINT?.replace(/\/$/, '')
const projectKey = import.meta.env.VITE_ARIADNE_PROJECT_KEY

export function initAriadne(): void {
  if (!endpoint || !projectKey) return
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
    // Аналитика не имеет права ломать загрузку приложения.
  }
}

export function trackAriadne(name: string, properties?: AriadneProperties): void {
  try {
    window.ariadne?.track(name, properties)
  } catch {
    // См. выше: любые сбои аналитики проглатываются.
  }
}

/** Анонимный ID визита — им серверное событие регистрации склеивается с браузерной воронкой. */
export function ariadneAnonymousId(): string | undefined {
  try {
    return window.ariadne?.getAnonymousId()
  } catch {
    return undefined
  }
}
