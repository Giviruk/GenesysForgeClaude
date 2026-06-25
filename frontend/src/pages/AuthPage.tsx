import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth-context'
import { api } from '../api/client'
import { GoogleSignInButton } from '../components/GoogleSignInButton'
import { Footer } from '../components/Footer'
import { navigate, usePath } from '../router'
import { peekReturnTo } from '../session'

type Mode = 'login' | 'register' | 'reset-request' | 'reset-confirm'

export function AuthPage() {
  const { login, register, loginWithGoogle, sessionExpired } = useAuth()
  const path = usePath()
  // Токен сброса приходит в письме как ?token=… — читаем один раз при открытии.
  const [resetToken] = useState(() => new URLSearchParams(window.location.search).get('token'))
  // Сброс пароля — оверлей поверх URL-режима login/register.
  const [resetMode, setResetMode] = useState<'reset-request' | 'reset-confirm' | null>(
    resetToken ? 'reset-confirm' : null)
  const mode: Mode = resetMode ?? (path === '/register' ? 'register' : 'login')
  // Стартовый экран — список способов входа; форма открывается по выбору способа.
  // Сразу показываем форму, если это глубокая ссылка (регистрация/сброс) или истёкшая сессия.
  const [showForm, setShowForm] = useState(
    () => !!resetToken || path === '/register' || sessionExpired)
  // Куда вернёмся после повторного входа (если сессия истекла на конкретном экране).
  const returnTo = sessionExpired ? peekReturnTo() : null
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  // client id Google, если вход через Google настроен на сервере.
  const [googleClientId, setGoogleClientId] = useState<string | null>(null)

  useEffect(() => {
    api.authProviders()
      .then(p => setGoogleClientId(p.googleClientId || null))
      .catch(() => { /* провайдеры не критичны — просто не показываем кнопку */ })
  }, [])

  // Возврат к входу/регистрации сбрасывает оверлей сброса и меняет URL.
  function goAuth(to: '/login' | '/register') {
    setResetMode(null)
    setError(null)
    setInfo(null)
    navigate(to)
  }

  // Открыть форму выбранным способом со стартового экрана.
  function openForm(to: '/login' | '/register') {
    goAuth(to)
    setShowForm(true)
  }

  // Вернуться со формы на стартовый экран со способами входа.
  function backToLanding() {
    setResetMode(null)
    setError(null)
    setInfo(null)
    setShowForm(false)
    navigate('/login')
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setInfo(null)
    setBusy(true)
    try {
      if (mode === 'login') {
        await login(email, password)
      } else if (mode === 'register') {
        await register(email, password, displayName)
      } else if (mode === 'reset-request') {
        await api.requestPasswordReset(email)
        // Всегда одинаковый ответ — не раскрываем, есть ли такой аккаунт.
        setInfo('Если аккаунт с таким e-mail существует, мы отправили ссылку для сброса пароля.')
      } else {
        await api.confirmPasswordReset(resetToken!, password)
        // Убираем токен из адреса и возвращаем на вход.
        window.history.replaceState(null, '', window.location.pathname)
        setResetMode(null)
        setPassword('')
        setInfo('Пароль обновлён — войдите с новым паролем.')
        navigate('/login')
      }
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

  const submitLabel =
    mode === 'login' ? 'Войти'
      : mode === 'register' ? 'Создать аккаунт'
        : mode === 'reset-request' ? 'Отправить ссылку'
          : 'Сохранить пароль'

  // ── Стартовый экран: бренд сверху, способы входа стопкой, подпись снизу. ──
  if (!showForm) {
    return (
      <div className="auth-page">
        <div className="auth-card auth-landing">
          <h1 className="logo auth-brand">Genesys Forge</h1>
          <p className="muted auth-brand-sub">Листы персонажей для Genesys Core и Realms of Terrinoth</p>

          {sessionExpired && (
            <div className="notice warn">Сессия истекла — войдите снова.</div>
          )}
          {error && <div className="error">{error}</div>}

          <div className="auth-methods">
            {googleClientId && (
              <GoogleSignInButton clientId={googleClientId}
                onCredential={onGoogleCredential} onError={setError} />
            )}
            <button className="primary auth-method" type="button" onClick={() => openForm('/login')}>
              Войти по e-mail
            </button>
            <button className="auth-method" type="button" onClick={() => openForm('/register')}>
              Создать аккаунт
            </button>
          </div>

          <p className="auth-foot muted small-text">
            Некоммерческий фанатский инструмент для Genesys от Fantasy Flight Games.
          </p>
        </div>
        <Footer />
      </div>
    )
  }

  // ── Форма выбранного способа (e-mail вход / регистрация / сброс пароля). ──
  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="logo auth-brand">Genesys Forge</h1>
        <p className="muted">Листы персонажей для Genesys Core и Realms of Terrinoth</p>

        {sessionExpired && mode === 'login' && !info && (
          <div className="notice warn">
            Сессия истекла — войдите снова.
            {returnTo && ' После входа вернётесь на открытую страницу.'}
          </div>
        )}
        {info && <div className="notice">{info}</div>}

        {(mode === 'login' || mode === 'register') && (
          <div className="tabs">
            <button className={mode === 'login' ? 'tab active' : 'tab'} onClick={() => goAuth('/login')}>Вход</button>
            <button className={mode === 'register' ? 'tab active' : 'tab'} onClick={() => goAuth('/register')}>Регистрация</button>
          </div>
        )}

        {mode === 'reset-request' && <h2 className="auth-title">Восстановление пароля</h2>}
        {mode === 'reset-confirm' && <h2 className="auth-title">Новый пароль</h2>}

        <form onSubmit={submit}>
          {mode === 'register' && (
            <label>
              Имя пользователя
              <input value={displayName} onChange={e => setDisplayName(e.target.value)} required minLength={1} />
            </label>
          )}

          {(mode === 'login' || mode === 'register' || mode === 'reset-request') && (
            <label>
              E-mail
              <input type="email" value={email} onChange={e => setEmail(e.target.value)} required />
            </label>
          )}

          {(mode === 'login' || mode === 'register') && (
            <label>
              Пароль
              <input type="password" value={password} onChange={e => setPassword(e.target.value)} required minLength={6} />
            </label>
          )}

          {mode === 'reset-confirm' && (
            <label>
              Новый пароль
              <input type="password" value={password} onChange={e => setPassword(e.target.value)} required minLength={6} />
            </label>
          )}

          {mode === 'reset-request' && (
            <p className="hint">Введите e-mail аккаунта — мы пришлём ссылку для установки нового пароля.</p>
          )}

          {error && <div className="error">{error}</div>}
          <button className="primary" type="submit" disabled={busy}>{submitLabel}</button>
        </form>

        <div className="auth-links">
          {mode === 'login' && (
            <button className="linklike" type="button" onClick={() => { setResetMode('reset-request'); setError(null); setInfo(null) }}>
              Забыли пароль?
            </button>
          )}
          {(mode === 'reset-request' || mode === 'reset-confirm') && (
            <button className="linklike" type="button" onClick={() => goAuth('/login')}>
              ← Вернуться ко входу
            </button>
          )}
          <button className="linklike" type="button" onClick={backToLanding}>
            ← Все способы входа
          </button>
        </div>

        {googleClientId && (
          <div className="auth-divider-block">
            <div className="auth-divider"><span>или</span></div>
            <GoogleSignInButton clientId={googleClientId}
              onCredential={onGoogleCredential} onError={setError} />
          </div>
        )}
      </div>
      <Footer />
    </div>
  )
}
