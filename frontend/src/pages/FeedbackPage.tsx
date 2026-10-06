import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { Footer } from '../components/Footer'
import { navigate } from '../router'
import { t } from '../i18n'

const MAX_LENGTH = 4000

/** Скрытое поле-ловушка: человек его не видит и не заполняет, бот заполняет все поля подряд. */
const HONEYPOT_STYLE = { position: 'absolute', left: '-10000px', width: 1, height: 1, overflow: 'hidden' } as const

/** Публичная форма обратной связи: сообщение уходит письмом на адрес поддержки. */
export function FeedbackPage({ loggedIn }: { loggedIn: boolean }) {
  const [message, setMessage] = useState('')
  const [email, setEmail] = useState('')
  const [website, setWebsite] = useState('')
  const [busy, setBusy] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const from = new URLSearchParams(window.location.search).get('from') ?? undefined

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await api.sendFeedback({ message, email: email.trim() || undefined, page: from, website: website || undefined })
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Не удалось отправить сообщение', 'Failed to send the message'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page about-page">
      <div className="panel">
        <div className="page-head">
          <h2>{t('Обратная связь', 'Feedback')}</h2>
          <button className="small" onClick={() => navigate(from ?? (loggedIn ? '/characters' : '/login'))}>
            {t('← Назад', '← Back')}
          </button>
        </div>

        {sent ? (
          <p>{t('Спасибо! Сообщение отправлено.', 'Thank you! Your message has been sent.')}</p>
        ) : (
          <form onSubmit={e => void submit(e)}>
            <p className="hint">
              {t(
                'Расскажите об ошибке, предложите идею или задайте вопрос. Сообщение придёт команде проекта на почту.',
                'Report a bug, suggest an idea or ask a question. The message is emailed to the project team.',
              )}
            </p>
            <label>
              {t('Сообщение', 'Message')}
              <textarea required minLength={5} maxLength={MAX_LENGTH} rows={8} value={message}
                onChange={e => setMessage(e.target.value)} />
            </label>
            <label>
              {t('Почта для ответа (необязательно)', 'Email for a reply (optional)')}
              <input type="email" maxLength={254} value={email} onChange={e => setEmail(e.target.value)} />
            </label>
            {loggedIn && (
              <p className="hint small-text">
                {t('Если не укажете почту, ответим на адрес вашего аккаунта.', 'If you leave it empty, we will reply to your account email.')}
              </p>
            )}
            <div style={HONEYPOT_STYLE} aria-hidden="true">
              <label>Website<input tabIndex={-1} autoComplete="off" value={website} onChange={e => setWebsite(e.target.value)} /></label>
            </div>
            {error && <div className="error">{error}</div>}
            <button className="primary" type="submit" disabled={busy || message.trim().length < 5}>
              {busy ? t('Отправка…', 'Sending…') : t('Отправить', 'Send')}
            </button>
          </form>
        )}
      </div>

      <Footer />
    </div>
  )
}
