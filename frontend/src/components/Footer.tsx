import { analyticsConfigured, openAnalyticsPreferences } from '../analytics/ariadne'
import { navigate } from '../router'
import { t, useLang } from '../i18n'

/** Глобальный футер: справка, About, обратная связь, переключатель языка и копирайт-дисклеймер. */
export function Footer() {
  const [lang, setLang] = useLang()
  return (
    <footer className="app-footer">
      <nav className="footer-links">
        <a href="/privacy">{t('Политика конфиденциальности', 'Privacy policy')}</a>
        <a href="/terms">{t('Соглашение', 'Terms')}</a>
        {analyticsConfigured() && <button className="linklike" type="button" onClick={openAnalyticsPreferences}>{t('Аналитика', 'Analytics')}</button>}
        <button className="linklike" type="button" onClick={() => navigate('/help')}>{t('Справка', 'Help')}</button>
        <button className="linklike" type="button" onClick={() => navigate('/about')}>{t('О проекте', 'About')}</button>
        <button className="linklike" type="button" onClick={() => navigate(`/feedback?from=${encodeURIComponent(window.location.pathname)}`)}>{t('Обратная связь', 'Feedback')}</button>
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
