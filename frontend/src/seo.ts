import { useEffect } from 'react'
import type { AppArea, AppRoute } from './router'

/**
 * SEO по маршрутам: title, meta description, canonical и og-теги обновляются
 * при клиентской навигации. Базовые значения заданы статически в index.html,
 * здесь — их уточнение под конкретную область приложения.
 */

const BASE_URL = 'https://genesys-forge.com'
const SITE = 'GenesysForge'

const DEFAULT_DESCRIPTION =
  'Интерактивный лист персонажа для НРИ-систем Genesys и Realms of Terrinoth: ' +
  'дайс-пулы, пирамида талантов, инвентарь, кампании с общим игровым столом, бестиарий и магия.'

const AREA_SEO: Record<AppArea, { title: string; description?: string }> = {
  characters: {
    title: `${SITE} — онлайн-лист персонажа для Genesys и Realms of Terrinoth`,
    description: DEFAULT_DESCRIPTION,
  },
  campaigns: { title: `Кампании — ${SITE}` },
  npcs: { title: `Бестиарий и NPC — ${SITE}` },
  magic: {
    title: `Магия Genesys — сборка магических действий — ${SITE}`,
    description:
      'Конструктор магических действий по правилам Genesys: атака, лечение, барьер и другие эффекты с модификаторами сложности.',
  },
  reference: {
    title: `Справочник Genesys — навыки, таланты, снаряжение — ${SITE}`,
    description:
      'Справочник по системам Genesys и Realms of Terrinoth: архетипы, карьеры, навыки, таланты, оружие, броня и снаряжение.',
  },
  help: {
    title: `Справка — как пользоваться ${SITE}`,
    description:
      'Как создать персонажа, купить таланты, собрать дайс-пул и вести кампанию в GenesysForge.',
  },
  about: {
    title: `О проекте — ${SITE}`,
    description:
      'GenesysForge — бесплатный некоммерческий инструмент для игроков и мастеров настольных ролевых систем Genesys и Realms of Terrinoth.',
  },
  account: { title: `Профиль — ${SITE}` },
  share: { title: `Лист персонажа — ${SITE}` },
}

/** Публичные без авторизации области — только они получают canonical и попадают в индекс. */
const INDEXABLE_PATHS = new Set(['/', '/about', '/help'])

function setMeta(selector: string, attr: 'content' | 'href', value: string): void {
  const el = document.head.querySelector<HTMLElement>(selector)
  if (el) el.setAttribute(attr, value)
}

export function applySeo(area: AppArea, path: string): void {
  const { title, description } = AREA_SEO[area] ?? AREA_SEO.characters
  document.title = title
  setMeta('meta[name="description"]', 'content', description ?? DEFAULT_DESCRIPTION)
  setMeta('meta[property="og:title"]', 'content', title)
  setMeta('meta[property="og:description"]', 'content', description ?? DEFAULT_DESCRIPTION)

  // canonical указывает на сам путь только для публичных страниц,
  // приватные области канонизируются на главную.
  const canonical = INDEXABLE_PATHS.has(path) ? `${BASE_URL}${path === '/' ? '/' : path}` : `${BASE_URL}/`
  setMeta('link[rel="canonical"]', 'href', canonical)
  setMeta('meta[property="og:url"]', 'content', canonical)
}

/** Хук: применяет SEO-теги при каждой смене маршрута. */
export function useSeo(route: AppRoute, path: string): void {
  const area = route.area
  useEffect(() => {
    applySeo(area, path)
  }, [area, path])
}
