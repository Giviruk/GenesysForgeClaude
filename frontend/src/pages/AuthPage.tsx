import { useState, type FormEvent } from 'react'
import { useAuth } from '../auth-context'
import { api } from '../api/client'

export function AuthPage() {
  const { login, register, sessionExpired } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setInfo(null)
    setBusy(true)
    try {
      if (mode === 'login') await login(email, password)
      else await register(email, password, displayName)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Неизвестная ошибка')
    } finally {
      setBusy(false)
    }
  }

  async function resendConfirmation() {
    setError(null)
    if (!email) { setInfo('Введите e-mail, чтобы выслать письмо подтверждения.'); return }
    try {
      await api.resendEmailConfirmation(email)
      // Всегда одинаковый ответ — не раскрываем наличие/статус аккаунта.
      setInfo('Если e-mail не подтверждён, мы выслали новую ссылку подтверждения.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не удалось отправить письмо.')
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="logo">Genesys Forge</h1>
        <p className="muted">Листы персонажей для Genesys Core и Realms of Terrinoth</p>
        {sessionExpired && mode === 'login' && !info && (
          <div className="notice warn">Сессия истекла. Пожалуйста, войдите снова.</div>
        )}
        {info && <div className="notice">{info}</div>}
        <div className="tabs">
          <button className={mode === 'login' ? 'tab active' : 'tab'} onClick={() => { setMode('login'); setInfo(null) }}>Вход</button>
          <button className={mode === 'register' ? 'tab active' : 'tab'} onClick={() => { setMode('register'); setInfo(null) }}>Регистрация</button>
        </div>
        <form onSubmit={submit}>
          {mode === 'register' && (
            <label>
              Имя пользователя
              <input value={displayName} onChange={e => setDisplayName(e.target.value)} required minLength={1} />
            </label>
          )}
          <label>
            E-mail
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} required />
          </label>
          <label>
            Пароль
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} required minLength={6} />
          </label>
          {error && <div className="error">{error}</div>}
          <button className="primary" type="submit" disabled={busy}>
            {mode === 'login' ? 'Войти' : 'Создать аккаунт'}
          </button>
        </form>
        {mode === 'login' && (
          <div className="auth-links">
            <button className="linklike" type="button" onClick={() => void resendConfirmation()}>
              Письмо для подтверждения e-mail не пришло? Отправить повторно
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
