import { useEffect, useState } from 'react'

/**
 * Минимальный клиентский роутер на History API без внешних зависимостей.
 * Используется для deep links на персонажа/кампанию/NPC и для области магии.
 * SPA-fallback на /index.html уже настроен в nginx и dev-сервере Vite,
 * поэтому прямой переход/refresh по любому пути отдаёт приложение.
 */

type Listener = () => void
const listeners = new Set<Listener>()

/** Текущий путь приложения (pathname без query/hash). */
export function currentPath(): string {
  return window.location.pathname || '/'
}

/**
 * Переход по клиентскому маршруту. По умолчанию добавляет запись в историю
 * (`pushState`), `replace: true` — заменяет текущую (без новой записи «назад»).
 */
export function navigate(to: string, opts: { replace?: boolean } = {}): void {
  if (to === currentPath() && !opts.replace) return
  if (opts.replace) window.history.replaceState(null, '', to)
  else window.history.pushState(null, '', to)
  // pushState/replaceState не шлют popstate — уведомляем подписчиков сами.
  listeners.forEach(l => l())
}

/** Подписка на текущий путь; перерисовывает компонент при навигации и кнопках браузера. */
export function usePath(): string {
  const [path, setPath] = useState(currentPath)
  useEffect(() => {
    const update = () => setPath(currentPath())
    listeners.add(update)
    window.addEventListener('popstate', update)
    return () => {
      listeners.delete(update)
      window.removeEventListener('popstate', update)
    }
  }, [])
  return path
}

export type AppArea = 'characters' | 'campaigns' | 'npcs' | 'magic'

export interface AppRoute {
  area: AppArea
  /** id выбранной сущности для characters/campaigns/npcs, иначе null */
  id: string | null
  /** путь не распознан — показываем «не найдено» */
  unknown: boolean
}

const ENTITY_AREAS: AppArea[] = ['characters', 'campaigns', 'npcs']

/**
 * Разбор pathname в маршрут приложения. Поддержанные пути:
 *   /                       → характеры (по умолчанию)
 *   /login | /register      → характеры (экран авторизации обрабатывается отдельно)
 *   /characters[/:id]
 *   /campaigns[/:id]
 *   /npcs[/:id]
 *   /magic
 */
export function parseRoute(pathname: string): AppRoute {
  const segments = pathname.replace(/^\/+|\/+$/g, '').split('/').filter(Boolean)
  if (segments.length === 0) return { area: 'characters', id: null, unknown: false }

  const [head, second] = segments
  if (head === 'login' || head === 'register') return { area: 'characters', id: null, unknown: false }
  if (head === 'magic') return { area: 'magic', id: null, unknown: segments.length > 1 }
  if (ENTITY_AREAS.includes(head as AppArea)) {
    return { area: head as AppArea, id: second ?? null, unknown: segments.length > 2 }
  }
  return { area: 'characters', id: null, unknown: true }
}
