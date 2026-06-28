import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Account } from '../api/types'

interface Props {
  onBack: () => void
}

/** Страница профиля / управления аккаунтом (U-21): имя, аватар, смена пароля. */
export function ProfilePage({ onBack }: Props) {
  const [account, setAccount] = useState<Account | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.account().then(setAccount).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Не удалось загрузить профиль'))
  }, [])

  if (error && !account) {
    return (
      <div className="page">
        <button onClick={onBack}>← Назад</button>
        <div className="error">{error}</div>
      </div>
    )
  }
  if (!account) {
    return <div className="page"><button onClick={onBack}>← Назад</button><p className="muted">Загрузка…</p></div>
  }

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <button onClick={onBack}>← Назад</button>
          <h2 className="inline-title">Профиль</h2>
        </div>
      </div>

      <ProfileForm account={account} onSaved={setAccount} />
      <PasswordForm />
    </div>
  )
}

const initials = (name: string) =>
  name.trim().split(/\s+/).map(p => p[0]).join('').slice(0, 2).toUpperCase() || '?'

function ProfileForm({ account, onSaved }: { account: Account; onSaved: (a: Account) => void }) {
  const [displayName, setDisplayName] = useState(account.displayName)
  const [avatarUrl, setAvatarUrl] = useState(account.avatarUrl ?? '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true); setError(null); setSaved(false)
    try {
      const next = await api.updateAccount({ displayName: displayName.trim(), avatarUrl: avatarUrl.trim() })
      onSaved(next)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка сохранения')
    } finally { setBusy(false) }
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <h3>Учётная запись</h3>
      <div className="profile-identity">
        {avatarUrl.trim()
          ? <img className="avatar lg" src={avatarUrl} alt="Аватар"
              onError={e => { (e.target as HTMLImageElement).style.display = 'none' }} />
          : <span className="avatar lg initials">{initials(displayName)}</span>}
        <div className="muted">{account.email}</div>
      </div>
      <label>Отображаемое имя
        <input value={displayName} onChange={e => setDisplayName(e.target.value)} required />
      </label>
      <label>URL аватара (необязательно)
        <input value={avatarUrl} onChange={e => setAvatarUrl(e.target.value)}
          placeholder="https://… (пусто — показываются инициалы)" />
      </label>
      {error && <div className="error">{error}</div>}
      {saved && <div className="hint">Сохранено.</div>}
      <button className="primary" type="submit" disabled={busy || !displayName.trim()}>Сохранить</button>
    </form>
  )
}

function PasswordForm() {
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null); setDone(false)
    if (next.length < 6) { setError('Новый пароль должен быть не короче 6 символов.'); return }
    if (next !== confirm) { setError('Пароли не совпадают.'); return }
    setBusy(true)
    try {
      await api.changePassword(current, next)
      setCurrent(''); setNext(''); setConfirm('')
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка смены пароля')
    } finally { setBusy(false) }
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <h3>Смена пароля</h3>
      <p className="hint">Смена пароля завершит все остальные сессии; текущая останется активной.</p>
      <label>Текущий пароль
        <input type="password" value={current} onChange={e => setCurrent(e.target.value)} required autoComplete="current-password" />
      </label>
      <label>Новый пароль
        <input type="password" value={next} onChange={e => setNext(e.target.value)} required autoComplete="new-password" />
      </label>
      <label>Повтор нового пароля
        <input type="password" value={confirm} onChange={e => setConfirm(e.target.value)} required autoComplete="new-password" />
      </label>
      {error && <div className="error">{error}</div>}
      {done && <div className="hint">Пароль изменён.</div>}
      <button className="primary" type="submit" disabled={busy || !current || !next || !confirm}>Изменить пароль</button>
    </form>
  )
}
