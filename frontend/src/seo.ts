import { useEffect } from 'react'
import { t } from './i18n'
import type { AppArea, AppRoute } from './router'

/**
 * SEO по маршрутам: title, meta description, canonical и og-теги обновляются
 * при клиентской навигации. Базовые значения заданы статически в index.html,
 * здесь — их уточнение под конкретную область приложения.
 */

const BASE_URL = 'https://genesys-forge.com'
const SITE = 'GenesysForge'

const DEFAULT_DESCRIPTION = t(
  'Интерактивный лист персонажа для НРИ-систем Genesys и Realms of Terrinoth: ' +
  'дайс-пулы, пирамида талантов, инвентарь, кампании с общим игровым столом, бестиарий и магия.',
  'Interactive character sheet for the Genesys and Realms of Terrinoth TTRPG systems: ' +
  'dice pools, talent pyramid, inventory, campaigns with a shared game table, bestiary and magic.',
)

const AREA_SEO: Record<AppArea, { title: string; description?: string }> = {
  characters: {
    title: t(
      `${SITE} — онлайн-лист персонажа для Genesys и Realms of Terrinoth`,
      `${SITE} — online character sheet for Genesys and Realms of Terrinoth`,
    ),
    description: DEFAULT_DESCRIPTION,
  },
  campaigns: { title: t(`Кампании — ${SITE}`, `Campaigns — ${SITE}`) },
  npcs: { title: t(`Бестиарий и NPC — ${SITE}`, `Bestiary and NPCs — ${SITE}`) },
  magic: {
    title: t(
      `Магия Genesys — сборка магических действий — ${SITE}`,
      `Genesys magic — build magic actions — ${SITE}`,
    ),
    description: t(
      'Конструктор магических действий по правилам Genesys: атака, лечение, барьер и другие эффекты с модификаторами сложности.',
      'Magic action builder for the Genesys rules: attack, heal, barrier and other effects with difficulty modifiers.',
    ),
  },
  reference: {
    title: t(
      `Справочник Genesys — навыки, таланты, снаряжение — ${SITE}`,
      `Genesys reference — skills, talents, gear — ${SITE}`,
    ),
    description: t(
      'Справочник по системам Genesys и Realms of Terrinoth: архетипы, карьеры, навыки, таланты, оружие, броня и снаряжение.',
      'Reference for the Genesys and Realms of Terrinoth systems: archetypes, careers, skills, talents, weapons, armor and gear.',
    ),
  },
  help: {
    title: t(`Справка — как пользоваться ${SITE}`, `Help — how to use ${SITE}`),
    description: t(
      'Как создать персонажа, купить таланты, собрать дайс-пул и вести кампанию в GenesysForge.',
      'How to create a character, buy talents, build a dice pool and run a campaign in GenesysForge.',
    ),
  },
  about: {
    title: t(`О проекте — ${SITE}`, `About — ${SITE}`),
    description: t(
      'GenesysForge — бесплатный некоммерческий инструмент для игроков и мастеров настольных ролевых систем Genesys и Realms of Terrinoth.',
      'GenesysForge is a free non-commercial tool for players and game masters of the Genesys and Realms of Terrinoth tabletop RPG systems.',
    ),
  },
  account: { title: t(`Профиль — ${SITE}`, `Profile — ${SITE}`) },
  share: { title: t(`Лист персонажа — ${SITE}`, `Character sheet — ${SITE}`) },
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
