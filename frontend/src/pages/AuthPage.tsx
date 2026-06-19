import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth-context'
import { api } from '../api/client'
import { GoogleSignInButton } from '../components/GoogleSignInButton'
import { navigate, usePath } from '../router'

export function AuthPage() {
  const { login, register, loginWithGoogle, sessionExpired } = useAuth()
  const path = usePath()
  const mode: 'login' | 'register' = path === '/register' ? 'register' : 'login'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  // client id Google, если вход через Google настроен на сервере.
  const [googleClientId, setGoogleClientId] = useState<string | null>(null)

  useEffect(() => {
    api.authProviders()
      .then(p => setGoogleClientId(p.googleClientId || null))
      .catch(() => { /* провайдеры не критичны — просто не показываем кнопку */ })
  }, [])

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

  const onGoogleCredential = useCallback(async (idToken: string) => {
    setError(null)
    try {
      await loginWithGoogle(idToken)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не удалось войти через Google')
    }
  }, [loginWithGoogle])

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="logo">Genesys Forge</h1>
        <p className="muted">Листы персонажей для Genesys Core и Realms of Terrinoth</p>
        {sessionExpired && mode === 'login' && (
          <div className="notice warn">Сессия истекла. Пожалуйста, войдите снова.</div>
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
        {googleClientId && (
          <div className="auth-divider-block">
            <div className="auth-divider"><span>или</span></div>
            <GoogleSignInButton clientId={googleClientId}
              onCredential={onGoogleCredential} onError={setError} />
          </div>
        )}
      </div>
    </div>
  )
}
