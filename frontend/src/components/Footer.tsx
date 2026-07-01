import { navigate } from '../router'

export const REPO_URL = 'https://github.com/Giviruk/GenesysForgeClaude'
export const CHANGELOG_URL = `${REPO_URL}/blob/master/CHANGELOG.md`

/** Глобальный футер: ссылки на About/changelog/исходники и копирайт-дисклеймер. */
export function Footer() {
  return (
    <footer className="app-footer">
      <nav className="footer-links">
        <button className="linklike" type="button" onClick={() => navigate('/help')}>Справка</button>
        <button className="linklike" type="button" onClick={() => navigate('/about')}>О проекте</button>
        <a className="linklike" href={CHANGELOG_URL} target="_blank" rel="noreferrer">Changelog</a>
        <a className="linklike" href={REPO_URL} target="_blank" rel="noreferrer">Исходный код</a>
      </nav>
      <p className="muted small-text footer-disclaimer">
        Неофициальный фан-проект, не аффилирован с Fantasy Flight Games. Код — под Apache-2.0.
        Genesys и Realms of Terrinoth — товарные знаки их правообладателей; официальные тексты книг не используются.
      </p>
    </footer>
  )
}
