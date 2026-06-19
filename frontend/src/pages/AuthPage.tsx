import { useState, type FormEvent } from 'react'
import { useAuth } from '../auth-context'
import { navigate, usePath } from '../router'
import { peekReturnTo } from '../session'

export function AuthPage() {
  const { login, register, sessionExpired } = useAuth()
  const path = usePath()
  const mode: 'login' | 'register' = path === '/register' ? 'register' : 'login'
  // Куда вернёмся после повторного входа (если сессия истекла на конкретном экране).
  const returnTo = sessionExpired ? peekReturnTo() : null
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
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

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="logo">Genesys Forge</h1>
        <p className="muted">Листы персонажей для Genesys Core и Realms of Terrinoth</p>
        {sessionExpired && mode === 'login' && (
          <div className="notice warn">
            Сессия истекла — войдите снова.
            {returnTo && ' После входа вернётесь на открытую страницу.'}
          </div>
        )}
        <div className="tabs">
          <button className={mode === 'login' ? 'tab active' : 'tab'} onClick={() => { setError(null); navigate('/login') }}>Вход</button>
          <button className={mode === 'register' ? 'tab active' : 'tab'} onClick={() => { setError(null); navigate('/register') }}>Регистрация</button>
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
      </div>
    </div>
  )
}
