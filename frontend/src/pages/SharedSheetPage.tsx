import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet } from '../api/types'
import { navigate } from '../router'
import { SYSTEM_LABELS } from '../utils/labels'
import { CharacterSheetPrint } from '../components/print/CharacterSheetPrint'

export function SharedSheetPage({ token, loggedIn }: { token: string; loggedIn: boolean }) {
  const [sheet, setSheet] = useState<CharacterSheet | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    api.sharedSheet(token)
      .then(data => { if (!cancelled) setSheet(data) })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Ссылка недоступна')
      })
    return () => { cancelled = true }
  }, [token])

  return (
    <div className="page shared-sheet-page">
      <div className="page-head no-print">
        <div>
          <button onClick={() => navigate(loggedIn ? '/characters' : '/login')}>
            {loggedIn ? '← К персонажам' : '← Войти'}
          </button>
          <h2 className="inline-title">Публичный лист персонажа</h2>
          {sheet && <span className={`badge ${sheet.system}`}>{SYSTEM_LABELS[sheet.system]}</span>}
        </div>
        <div className="head-actions">
          <button className="small" onClick={() => window.print()} disabled={!sheet}>Печать</button>
        </div>
      </div>

      {error && (
        <div className="panel">
          <h2>Ссылка недоступна</h2>
          <div className="error">{error}</div>
          <p className="muted">Возможно, ссылка была отозвана владельцем персонажа.</p>
        </div>
      )}
      {!error && !sheet && <p className="muted">Загрузка…</p>}
      {sheet && <CharacterSheetPrint sheet={sheet} loadNotes={false} />}
    </div>
  )
}
