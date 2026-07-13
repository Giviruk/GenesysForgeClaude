import { navigate } from '../router'
import { t, useLang } from '../i18n'

export const REPO_URL = 'https://github.com/Giviruk/GenesysForgeClaude'
export const CHANGELOG_URL = `${REPO_URL}/blob/master/CHANGELOG.md`

/** Глобальный футер: ссылки на About/changelog/исходники, переключатель языка и копирайт-дисклеймер. */
export function Footer() {
  const [lang, setLang] = useLang()
  return (
    <footer className="app-footer">
      <nav className="footer-links">
        <button className="linklike" type="button" onClick={() => navigate('/help')}>{t('Справка', 'Help')}</button>
        <button className="linklike" type="button" onClick={() => navigate('/about')}>{t('О проекте', 'About')}</button>
        <a className="linklike" href={CHANGELOG_URL} target="_blank" rel="noreferrer">Changelog</a>
        <a className="linklike" href={REPO_URL} target="_blank" rel="noreferrer">{t('Исходный код', 'Source code')}</a>
        <button className="linklike" type="button" onClick={() => setLang(lang === 'ru' ? 'en' : 'ru')}>
          {lang === 'ru' ? 'English' : 'Русский'}
        </button>
      </nav>
      <p className="muted small-text footer-disclaimer">
        {t(
          'Неофициальный фан-проект, не аффилирован с Fantasy Flight Games. Код — под Apache-2.0. ' +
          'Genesys и Realms of Terrinoth — товарные знаки их правообладателей; официальные тексты книг не используются.',
          'Unofficial fan project, not affiliated with Fantasy Flight Games. Code is licensed under Apache-2.0. ' +
          'Genesys and Realms of Terrinoth are trademarks of their respective owners; no official book texts are used.',
        )}
      </p>
    </footer>
  )
}
