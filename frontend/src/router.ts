import { useEffect, useState } from 'react'

/**
 * Минимальный клиентский роутер на History API без внешних зависимостей.
 * Используется для deep links на персонажа/кампанию/NPC и для области магии.
 * SPA-fallback на /index.html уже настроен в nginx и dev-сервере Vite,
 * поэтому прямой переход/refresh по любому пути отдаёт приложение.
 */

type Listener = () => void
const listeners = new Set<Listener>()
const guards = new Set<(to: string) => boolean>()
let acceptedUrl = window.location.href
// Entry positions let a rejected traversal return in either direction without
// adding entries or discarding the browser's Forward history.
const HISTORY_INDEX = '__genesysForgeIndex'
function historyIndex(): number | null {
  const index: unknown = window.history.state?.[HISTORY_INDEX]
  return typeof index === 'number' && Number.isInteger(index) ? index : null
}
function historyState(index: number) {
  return { ...window.history.state, [HISTORY_INDEX]: index }
}
let acceptedIndex = historyIndex() ?? 0
let restoring = false
window.history.replaceState(historyState(acceptedIndex), '', acceptedUrl)

// One popstate subscription checks guards before any React route subscription.
// Independent subscriptions can unmount an editor before its guard runs.
function onPopState() {
  const targetIndex = historyIndex()
  if (restoring && targetIndex === acceptedIndex && window.location.href === acceptedUrl) {
    restoring = false
    return
  }
  const to = window.location.pathname + window.location.search
  if ([...guards].some(guard => !guard(to))) {
    if (targetIndex !== null && targetIndex !== acceptedIndex) {
      restoring = true
      window.history.go(acceptedIndex - targetIndex)
    } else {
      // Legacy callers may replace the URL without the router's state marker.
      window.history.replaceState(historyState(acceptedIndex), '', acceptedUrl)
    }
    return
  }
  acceptedUrl = window.location.href
  acceptedIndex = targetIndex ?? acceptedIndex
  window.history.replaceState(historyState(acceptedIndex), '', acceptedUrl)
  listeners.forEach(l => l())
}
window.addEventListener('popstate', onPopState)
if (import.meta.hot) import.meta.hot.dispose(() => window.removeEventListener('popstate', onPopState))

/** Editors can prevent losing unsaved changes on shell links and browser Back. */
export function registerNavigationGuard(guard: (to: string) => boolean): () => void {
  guards.add(guard)
  acceptedUrl = window.location.href
  acceptedIndex = historyIndex() ?? acceptedIndex
  window.history.replaceState(historyState(acceptedIndex), '', acceptedUrl)
  return () => { guards.delete(guard) }
}

export function useSearch(): string {
  const [search, setSearch] = useState(() => window.location.search)
  useEffect(() => {
    const update = () => setSearch(window.location.search)
    listeners.add(update)
    return () => { listeners.delete(update) }
  }, [])
  return search
}

/** Текущий путь приложения (pathname без query/hash). */
export function currentPath(): string {
  return window.location.pathname || '/'
}

/**
 * Переход по клиентскому маршруту. По умолчанию добавляет запись в историю
 * (`pushState`), `replace: true` — заменяет текущую (без новой записи «назад»).
 */
export function navigate(to: string, opts: { replace?: boolean } = {}): void {
  if (restoring) return
  if (to === currentPath() + window.location.search && !opts.replace) return
  if ([...guards].some(guard => !guard(to))) return
  acceptedIndex = historyIndex() ?? acceptedIndex
  if (opts.replace) window.history.replaceState(historyState(acceptedIndex), '', to)
  else window.history.pushState(historyState(++acceptedIndex), '', to)
  acceptedUrl = window.location.href
  // pushState/replaceState не шлют popstate — уведомляем подписчиков сами.
  listeners.forEach(l => l())
}

/** Подписка на текущий путь; перерисовывает компонент при навигации и кнопках браузера. */
export function usePath(): string {
  const [path, setPath] = useState(currentPath)
  useEffect(() => {
    const update = () => setPath(currentPath())
    listeners.add(update)
    return () => {
      listeners.delete(update)
    }
  }, [])
  return path
}

export type AppArea = 'characters' | 'campaigns' | 'npcs' | 'magic' | 'shop' | 'reference' | 'library' | 'help' | 'about' | 'feedback' | 'account' | 'share' | 'privacy' | 'terms'

export interface AppRoute {
  area: AppArea
  /** id выбранной сущности для characters/campaigns/npcs, иначе null */
  id: string | null
  /** под-вью сущности: 'print' (характер), campaign views или campaign character sheet; иначе null */
  sub: string | null
  /** id под-сущности: энкаунтер или персонаж кампании; иначе null */
  subId: string | null
  /** путь не распознан — показываем «не найдено» */
  unknown: boolean
}

const ENTITY_AREAS: AppArea[] = ['characters', 'campaigns', 'npcs']

/** Допустимые под-вью по областям; для encounters разрешён ещё и id под-сущности (:eid). */
const SUBVIEWS: Record<string, { allowed: string[]; withId: string[] }> = {
  characters: { allowed: ['print'], withId: [] },
  campaigns: { allowed: ['chronicle', 'table', 'encounters', 'custom', 'content', 'settings', 'characters'], withId: ['encounters', 'characters'] },
  npcs: { allowed: ['edit'], withId: [] },
}

const base = (area: AppArea, id: string | null = null, unknown = false): AppRoute =>
  ({ area, id, sub: null, subId: null, unknown })

/**
 * Разбор pathname в маршрут приложения. Поддержанные пути:
 *   /                                          → характеры (по умолчанию)
 *   /login | /register                         → характеры (экран авторизации обрабатывается отдельно)
 *   /characters[/:id[/print]]
 *   /campaigns[/:id[/chronicle|table|encounters[/:eid]|characters/:characterId|custom|content|settings]]
 *   /npcs[/:id]
 *   /magic
 *   /shop
 *   /reference
 *   /help
 *   /about
 *   /feedback
 *   /share/:token                              → публичный read-only лист
 */
export function parseRoute(pathname: string): AppRoute {
  const segments = pathname.replace(/^\/+|\/+$/g, '').split('/').filter(Boolean)
  if (segments.length === 0) return base('characters')

  const [head, second, third, fourth] = segments
  if (head === 'login' || head === 'register') return base('characters')
  if (head === 'magic') return base('magic', null, segments.length > 1)
  if (head === 'shop') return base('shop', null, segments.length > 1)
  if (head === 'library') return base('library', null, segments.length > 1)
  if (head === 'reference') return base('reference', null, segments.length > 1)
  if (head === 'privacy' || head === 'terms') return base(head, null, segments.length > 1)
  if (head === 'help') return base('help', null, segments.length > 1)
  if (head === 'about') return base('about', null, segments.length > 1)
  if (head === 'feedback') return base('feedback', null, segments.length > 1)
  if (head === 'account') return base('account', null, segments.length > 1)
  if (head === 'share') return base('share', second ?? null, segments.length !== 2)
  if (!ENTITY_AREAS.includes(head as AppArea)) return base('characters', null, true)

  const area = head as AppArea
  if (area === 'npcs' && second === 'new') return { ...base(area), sub: 'new', unknown: segments.length > 2 }
  const id = second ?? null
  if (!id) return base(area)
  if (!third) return base(area, id)

  // Есть под-сегмент — проверяем, что он допустим для области.
  const sub = third
  const config = SUBVIEWS[area]
  const allowed = config.allowed.includes(sub)
  const allowsId = config.withId.includes(sub)
  const requiresId = area === 'campaigns' && sub === 'characters'
  const tooDeep = fourth ? !allowsId || segments.length > 4 : segments.length > 3
  if (!allowed || tooDeep || (requiresId && !fourth))
    return { area, id, sub: null, subId: null, unknown: true }

  return { area, id, sub, subId: fourth ?? null, unknown: false }
}
