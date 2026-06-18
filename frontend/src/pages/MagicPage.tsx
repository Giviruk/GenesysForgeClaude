import { useState } from 'react'
import type { GameSystem } from '../api/types'
import { SYSTEM_LABELS } from '../utils/labels'
import { MagicBuilder } from '../components/MagicBuilder'

/**
 * Отдельная страница Magic Action Builder для использования без персонажа (режим мастера, §5.2).
 * Система выбирается вручную; интеграция с листом персонажа доступна на вкладке «Магия» листа.
 */
export function MagicPage() {
  const [system, setSystem] = useState<GameSystem>('realmsOfTerrinoth')
  const [error, setError] = useState<string | null>(null)

  return (
    <div className="page">
      <div className="page-head">
        <h2>Сборка магии</h2>
        <div className="system-switch">
          {(['realmsOfTerrinoth', 'genesysCore'] as GameSystem[]).map(s => (
            <button key={s} className={system === s ? 'tab active' : 'tab'} onClick={() => setSystem(s)}>
              {SYSTEM_LABELS[s]}
            </button>
          ))}
        </div>
      </div>
      {error && <div className="error floating">{error}</div>}
      <MagicBuilder system={system} onError={setError} />
    </div>
  )
}
