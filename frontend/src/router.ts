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

export type AppArea = 'characters' | 'campaigns' | 'npcs' | 'magic' | 'reference' | 'about' | 'account'

export interface AppRoute {
  area: AppArea
  /** id выбранной сущности для characters/campaigns/npcs, иначе null */
  id: string | null
  /** под-вью сущности: 'print' (характер), 'table'/'handbook'/'encounters' (кампания); иначе null */
  sub: string | null
  /** id под-сущности: энкаунтер для /campaigns/:id/encounters/:eid; иначе null */
  subId: string | null
  /** путь не распознан — показываем «не найдено» */
  unknown: boolean
}

const ENTITY_AREAS: AppArea[] = ['characters', 'campaigns', 'npcs']

/** Допустимые под-вью по областям; для encounters разрешён ещё и id под-сущности (:eid). */
const SUBVIEWS: Record<string, { allowed: string[]; withId: string[] }> = {
  characters: { allowed: ['print'], withId: [] },
  campaigns: { allowed: ['table', 'handbook', 'encounters'], withId: ['encounters'] },
  npcs: { allowed: [], withId: [] },
}

const base = (area: AppArea, id: string | null = null, unknown = false): AppRoute =>
  ({ area, id, sub: null, subId: null, unknown })

/**
 * Разбор pathname в маршрут приложения. Поддержанные пути:
 *   /                                          → характеры (по умолчанию)
 *   /login | /register                         → характеры (экран авторизации обрабатывается отдельно)
 *   /characters[/:id[/print]]
 *   /campaigns[/:id[/table|handbook|encounters[/:eid]]]
 *   /npcs[/:id]
 *   /magic
 *   /reference
 *   /about
 */
export function parseRoute(pathname: string): AppRoute {
  const segments = pathname.replace(/^\/+|\/+$/g, '').split('/').filter(Boolean)
  if (segments.length === 0) return base('characters')

  const [head, second, third, fourth] = segments
  if (head === 'login' || head === 'register') return base('characters')
  if (head === 'magic') return base('magic', null, segments.length > 1)
  if (head === 'reference') return base('reference', null, segments.length > 1)
  if (head === 'about') return base('about', null, segments.length > 1)
  if (head === 'account') return base('account', null, segments.length > 1)
  if (!ENTITY_AREAS.includes(head as AppArea)) return base('characters', null, true)

  const area = head as AppArea
  const id = second ?? null
  if (!id) return base(area)
  if (!third) return base(area, id)

  // Есть под-сегмент — проверяем, что он допустим для области.
  const sub = third
  const config = SUBVIEWS[area]
  const allowed = config.allowed.includes(sub)
  const allowsId = config.withId.includes(sub)
  const tooDeep = fourth ? !allowsId || segments.length > 4 : segments.length > 3
  if (!allowed || tooDeep) return { area, id, sub: null, subId: null, unknown: true }

  return { area, id, sub, subId: fourth ?? null, unknown: false }
}
