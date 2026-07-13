import { afterEach, describe, expect, it, vi } from 'vitest'

// Язык фиксируется при загрузке модуля, поэтому каждый сценарий импортирует i18n заново.
async function loadI18n(stored: string | null, browserLang: string) {
  vi.resetModules()
  localStorage.clear()
  if (stored !== null) localStorage.setItem('genesysforge.lang', stored)
  vi.stubGlobal('navigator', { ...navigator, language: browserLang })
  return await import('./i18n')
}

afterEach(() => {
  vi.unstubAllGlobals()
  localStorage.clear()
  localStorage.setItem('genesysforge.lang', 'ru') // восстанавливаем фиксацию из test-setup
})

describe('detectLang', () => {
  it('берёт сохранённый выбор из localStorage', async () => {
    const { lang } = await loadI18n('en', 'ru-RU')
    expect(lang).toBe('en')
  })

  it('игнорирует мусорное значение в localStorage', async () => {
    const { lang } = await loadI18n('de', 'ru-RU')
    expect(lang).toBe('ru')
  })

  it('определяет русский по navigator.language', async () => {
    const { lang } = await loadI18n(null, 'ru')
    expect(lang).toBe('ru')
  })

  it('для прочих языков браузера выбирает английский', async () => {
    const { lang } = await loadI18n(null, 'en-US')
    expect(lang).toBe('en')
  })
})

describe('t', () => {
  it('возвращает вариант текущего языка', async () => {
    const ru = await loadI18n('ru', 'en-US')
    expect(ru.t('Сохранить', 'Save')).toBe('Сохранить')
    const en = await loadI18n('en', 'ru-RU')
    expect(en.t('Сохранить', 'Save')).toBe('Save')
  })
})

describe('setLang', () => {
  it('сохраняет выбор и перезагружает страницу', async () => {
    const { setLang } = await loadI18n('ru', 'ru-RU')
    const reload = vi.fn()
    vi.stubGlobal('location', { ...window.location, reload })
    setLang('en')
    expect(localStorage.getItem('genesysforge.lang')).toBe('en')
    expect(reload).toHaveBeenCalled()
  })

  it('ничего не делает при выборе текущего языка', async () => {
    const { setLang } = await loadI18n('ru', 'ru-RU')
    const reload = vi.fn()
    vi.stubGlobal('location', { ...window.location, reload })
    setLang('ru')
    expect(reload).not.toHaveBeenCalled()
  })
})
