import { useEffect, useRef, useState } from 'react'
import { api } from '../api/client'

/**
 * Обработка ссылки подтверждения e-mail из письма (?confirmToken=…).
 * Работает независимо от состояния авторизации — показывается до проверки токена сессии.
 */
export function ConfirmEmailPage({ token, onDone }: { token: string; onDone: () => void }) {
  const [status, setStatus] = useState<'pending' | 'ok' | 'error'>('pending')
  const [message, setMessage] = useState('Подтверждаем e-mail…')
  // В StrictMode эффект вызывается дважды — защищаемся от двойного запроса (токен одноразовый).
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true
    api.confirmEmail(token)
      .then(() => {
        setStatus('ok')
        setMessage('E-mail подтверждён. Спасибо!')
      })
      .catch((err: unknown) => {
        setStatus('error')
        setMessage(err instanceof Error ? err.message : 'Не удалось подтвердить e-mail.')
      })
  }, [token])

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="logo">Genesys Forge</h1>
        <h2 className="auth-title">Подтверждение e-mail</h2>
        <div className={status === 'error' ? 'notice warn' : 'notice'}>{message}</div>
        {status !== 'pending' && (
          <button className="primary" onClick={onDone}>Продолжить</button>
        )}
      </div>
    </div>
  )
}
