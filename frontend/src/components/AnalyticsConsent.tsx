import { useEffect, useState } from 'react'
import {
  ANALYTICS_CONSENT_KEY, ANALYTICS_CONSENT_CHANGED, ANALYTICS_PREFERENCES,
  analyticsConfigured, disableAriadne, initAriadne, readAnalyticsConsent, setAnalyticsConsent,
  type AnalyticsConsent as Consent,
} from '../analytics/ariadne'
import { t } from '../i18n'

export function AnalyticsConsent() {
  const [open, setOpen] = useState(() => analyticsConfigured() && readAnalyticsConsent() === null)
  const [error, setError] = useState(false)
  useEffect(() => {
    const show = () => { setOpen(true); setError(false) }
    const changed = () => setOpen(false)
    const storageChanged = (event: StorageEvent) => {
      if (event.key !== ANALYTICS_CONSENT_KEY && event.key !== null) return
      if (readAnalyticsConsent() === 'granted') initAriadne()
      else disableAriadne()
      setOpen(analyticsConfigured() && readAnalyticsConsent() === null)
    }
    window.addEventListener(ANALYTICS_PREFERENCES, show)
    window.addEventListener(ANALYTICS_CONSENT_CHANGED, changed)
    window.addEventListener('storage', storageChanged)
    return () => {
      window.removeEventListener(ANALYTICS_PREFERENCES, show)
      window.removeEventListener(ANALYTICS_CONSENT_CHANGED, changed)
      window.removeEventListener('storage', storageChanged)
    }
  }, [])
  if (!open) return null
  const choose = (consent: Consent) => {
    if (setAnalyticsConsent(consent)) setOpen(false)
    else setError(true)
  }
  return (
    <section className="analytics-consent no-print" role="region" aria-label={t('Согласие на аналитику', 'Analytics consent')}>
      <p>{t(
        'Разрешить сервису собирать статистику действий на genesys-forge.com?',
        'Allow the service to collect activity statistics on genesys-forge.com?',
      )} <a href="/privacy">{t('Политика конфиденциальности', 'Privacy policy')}</a></p>
      <div className="analytics-consent-actions">
        <button type="button" onClick={() => choose('granted')}>{t('Принять', 'Accept')}</button>
        <button type="button" onClick={() => choose('denied')}>{t('Отказаться', 'Decline')}</button>
      </div>
      {error && <p role="alert">{t('Не удалось сохранить выбор. Аналитика выключена.', 'Could not save your choice. Analytics remains off.')}</p>}
    </section>
  )
}
