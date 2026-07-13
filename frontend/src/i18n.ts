/**
 * Минимальная i18n-прослойка без внешних библиотек.
 *
 * Язык определяется один раз при загрузке страницы (localStorage → navigator.language)
 * и не меняется до перезагрузки: словари интерфейса (labels.ts, NAV_ITEMS и т.п.)
 * вычисляются как константы модулей, поэтому переключение языка выполняет reload.
 *
 * Переводы со-локализованы с местом использования: t('Сохранить', 'Save').
 * Игровой контент (данные с сервера) локализуется отдельно через localizedName/dualName.
 */

export type Lang = 'ru' | 'en'

const STORAGE_KEY = 'genesysforge.lang'

function detectLang(): Lang {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored === 'ru' || stored === 'en') return stored
  } catch {
    // localStorage может быть недоступен (приватный режим) — падаем на язык браузера.
  }
  const browser = typeof navigator !== 'undefined' ? navigator.language : ''
  return browser.toLowerCase().startsWith('ru') ? 'ru' : 'en'
}

/** Язык интерфейса, зафиксированный на всё время жизни страницы. */
export const lang: Lang = detectLang()

if (typeof document !== 'undefined') document.documentElement.lang = lang

/** Выбор значения по текущему языку — для строк и целых словарей: t({...}, {...}). */
export const t = <T>(ru: T, en: T): T => (lang === 'ru' ? ru : en)

/** Сохраняет выбор и перезагружает страницу (константы вычислены на языке загрузки). */
export function setLang(next: Lang): void {
  if (next === lang) return
  try {
    localStorage.setItem(STORAGE_KEY, next)
  } catch {
    // Не сохранится — просто переключим до конца сессии.
  }
  window.location.reload()
}

/** Хук для переключателя языка. */
export function useLang(): [Lang, (next: Lang) => void] {
  return [lang, setLang]
}
