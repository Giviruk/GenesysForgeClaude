import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet } from '../api/types'
import { navigate } from '../router'
import { SYSTEM_LABELS } from '../utils/labels'
import { t } from '../i18n'
import { CharacterSheetPrint } from '../components/print/CharacterSheetPrint'

export function SharedSheetPage({ token, loggedIn }: { token: string; loggedIn: boolean }) {
  const [sheet, setSheet] = useState<CharacterSheet | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    api.sharedSheet(token)
      .then(data => { if (!cancelled) setSheet(data) })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : t('Ссылка недоступна', 'Link unavailable'))
      })
    return () => { cancelled = true }
  }, [token])

  return (
    <div className="page shared-sheet-page">
      <div className="page-head no-print">
        <div>
          <button onClick={() => navigate(loggedIn ? '/characters' : '/login')}>
            {loggedIn ? t('← К персонажам', '← Back to characters') : t('← Войти', '← Sign in')}
          </button>
          <h2 className="inline-title">{t('Публичный лист персонажа', 'Public character sheet')}</h2>
          {sheet && <span className={`badge ${sheet.system}`}>{SYSTEM_LABELS[sheet.system]}</span>}
        </div>
        <div className="head-actions">
          <button className="small" onClick={() => window.print()} disabled={!sheet}>{t('Печать', 'Print')}</button>
        </div>
      </div>

      {error && (
        <div className="panel">
          <h2>{t('Ссылка недоступна', 'Link unavailable')}</h2>
          <div className="error">{error}</div>
          <p className="muted">{t('Возможно, ссылка была отозвана владельцем персонажа.', 'The link may have been revoked by the character’s owner.')}</p>
        </div>
      )}
      {!error && !sheet && <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
      {sheet && <CharacterSheetPrint sheet={sheet} loadNotes={false} />}
    </div>
  )
}
