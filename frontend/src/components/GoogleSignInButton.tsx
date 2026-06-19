import { useEffect, useRef } from 'react'

const GIS_SRC = 'https://accounts.google.com/gsi/client'

interface GoogleIdApi {
  initialize(config: { client_id: string; callback: (r: { credential?: string }) => void }): void
  renderButton(parent: HTMLElement, options: Record<string, unknown>): void
}
interface GoogleNamespace { accounts: { id: GoogleIdApi } }
declare global {
  interface Window { google?: GoogleNamespace }
}

/** Загружает скрипт Google Identity Services один раз. */
function loadGis(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.google?.accounts?.id) return resolve()
    const existing = document.querySelector<HTMLScriptElement>(`script[src="${GIS_SRC}"]`)
    if (existing) {
      existing.addEventListener('load', () => resolve())
      existing.addEventListener('error', () => reject(new Error('Не удалось загрузить Google Identity Services')))
      return
    }
    const s = document.createElement('script')
    s.src = GIS_SRC
    s.async = true
    s.defer = true
    s.onload = () => resolve()
    s.onerror = () => reject(new Error('Не удалось загрузить Google Identity Services'))
    document.head.appendChild(s)
  })
}

/**
 * Кнопка «Войти через Google». Показывается только при настроенном clientId
 * (см. /api/auth/providers). Получает ID-токен от GIS и отдаёт его наверх.
 */
export function GoogleSignInButton({ clientId, onCredential, onError }: {
  clientId: string
  onCredential: (idToken: string) => void
  onError: (message: string) => void
}) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let cancelled = false
    loadGis()
      .then(() => {
        if (cancelled || !ref.current || !window.google?.accounts?.id) return
        window.google.accounts.id.initialize({
          client_id: clientId,
          callback: (resp) => { if (resp.credential) onCredential(resp.credential) },
        })
        window.google.accounts.id.renderButton(ref.current, { theme: 'outline', size: 'large', width: 280 })
      })
      .catch((e: unknown) => onError(e instanceof Error ? e.message : 'Ошибка Google'))
    return () => { cancelled = true }
  }, [clientId, onCredential, onError])

  return <div ref={ref} className="google-btn" />
}
