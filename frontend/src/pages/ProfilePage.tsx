import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Account } from '../api/types'
import { t } from '../i18n'

/** Лимит размера изображения — зеркало серверного (5 МБ), для быстрой ошибки без запроса. */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024

interface Props {
  onBack: () => void
}

/** Страница профиля / управления аккаунтом (U-21): имя, аватар, смена пароля. */
export function ProfilePage({ onBack }: Props) {
  const [account, setAccount] = useState<Account | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.account().then(setAccount).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : t('Не удалось загрузить профиль', 'Failed to load profile')))
  }, [])

  if (error && !account) {
    return (
      <div className="page">
        <button onClick={onBack}>{t('← Назад', '← Back')}</button>
        <div className="error">{error}</div>
      </div>
    )
  }
  if (!account) {
    return <div className="page"><button onClick={onBack}>{t('← Назад', '← Back')}</button><p className="muted">{t('Загрузка…', 'Loading…')}</p></div>
  }

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <button onClick={onBack}>{t('← Назад', '← Back')}</button>
          <h2 className="inline-title">{t('Профиль', 'Profile')}</h2>
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
  const fileRef = useRef<HTMLInputElement>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true); setError(null); setSaved(false)
    try {
      const next = await api.updateAccount({ displayName: displayName.trim(), avatarUrl: avatarUrl.trim() })
      onSaved(next)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка сохранения', 'Failed to save'))
    } finally { setBusy(false) }
  }

  async function uploadFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = '' // повторный выбор того же файла снова вызывает onChange
    if (!file) return
    if (file.size > MAX_IMAGE_BYTES) { setError(t('Файл больше 5 МБ.', 'File is larger than 5 MB.')); return }
    setBusy(true); setError(null); setSaved(false)
    try {
      const next = await api.uploadAvatar(file)
      setAvatarUrl(next.avatarUrl ?? '')
      onSaved(next)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка загрузки файла', 'File upload failed'))
    } finally { setBusy(false) }
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <h3>{t('Учётная запись', 'Account')}</h3>
      <div className="profile-identity">
        {avatarUrl.trim()
          ? <img className="avatar lg" src={avatarUrl} alt={t('Аватар', 'Avatar')}
              onError={e => { (e.target as HTMLImageElement).style.display = 'none' }} />
          : <span className="avatar lg initials">{initials(displayName)}</span>}
        <div className="muted">{account.email}</div>
      </div>
      <label>{t('Отображаемое имя', 'Display name')}
        <input value={displayName} onChange={e => setDisplayName(e.target.value)} required />
      </label>
      <label>{t('URL аватара (необязательно)', 'Avatar URL (optional)')}
        <input value={avatarUrl} onChange={e => setAvatarUrl(e.target.value)}
          placeholder={t('https://… (пусто — показываются инициалы)', 'https://… (empty — initials are shown)')} />
      </label>
      <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/webp" hidden
        data-testid="avatar-file" onChange={uploadFile} />
      <button type="button" disabled={busy} onClick={() => fileRef.current?.click()}>
        {t('Загрузить файл (JPEG/PNG/WebP, до 5 МБ)', 'Upload file (JPEG/PNG/WebP, up to 5 MB)')}
      </button>
      {error && <div className="error">{error}</div>}
      {saved && <div className="hint">{t('Сохранено.', 'Saved.')}</div>}
      <button className="primary" type="submit" disabled={busy || !displayName.trim()}>{t('Сохранить', 'Save')}</button>
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
    if (next.length < 6) { setError(t('Новый пароль должен быть не короче 6 символов.', 'The new password must be at least 6 characters.')); return }
    if (next !== confirm) { setError(t('Пароли не совпадают.', 'Passwords do not match.')); return }
    setBusy(true)
    try {
      await api.changePassword(current, next)
      setCurrent(''); setNext(''); setConfirm('')
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка смены пароля', 'Failed to change password'))
    } finally { setBusy(false) }
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <h3>{t('Смена пароля', 'Change password')}</h3>
      <p className="hint">{t('Смена пароля завершит все остальные сессии; текущая останется активной.', 'Changing the password ends all other sessions; the current one stays active.')}</p>
      <label>{t('Текущий пароль', 'Current password')}
        <input type="password" value={current} onChange={e => setCurrent(e.target.value)} required autoComplete="current-password" />
      </label>
      <label>{t('Новый пароль', 'New password')}
        <input type="password" value={next} onChange={e => setNext(e.target.value)} required autoComplete="new-password" />
      </label>
      <label>{t('Повтор нового пароля', 'Repeat new password')}
        <input type="password" value={confirm} onChange={e => setConfirm(e.target.value)} required autoComplete="new-password" />
      </label>
      {error && <div className="error">{error}</div>}
      {done && <div className="hint">{t('Пароль изменён.', 'Password changed.')}</div>}
      <button className="primary" type="submit" disabled={busy || !current || !next || !confirm}>{t('Изменить пароль', 'Change password')}</button>
    </form>
  )
}
