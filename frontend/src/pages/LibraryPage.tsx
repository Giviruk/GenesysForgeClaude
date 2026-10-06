import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { GameSystem, Reference } from '../api/types'
import { CustomTab } from '../components/CustomTab'
import { SYSTEM_LABELS } from '../utils/labels'
import { t } from '../i18n'

export function LibraryPage() {
  const [system, setSystem] = useState<GameSystem>('genesysCore')
  const [loaded, setLoaded] = useState<{ system: GameSystem; reference: Reference } | null>(null)
  const [error, setError] = useState<string | null>(null)
  const refresh = useCallback(async () => {
    const reference = await api.reference(system)
    setLoaded({ system, reference })
  }, [system])

  useEffect(() => {
    let cancelled = false
    api.reference(system).then(reference => {
      if (!cancelled) { setLoaded({ system, reference }); setError(null) }
    }).catch((err: unknown) => {
      if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    })
    return () => { cancelled = true }
  }, [system])

  return <div className="page">
    <h2>{t('Моя библиотека', 'My library')}</h2>
    <div className="system-switch">
      {(['genesysCore', 'realmsOfTerrinoth'] as GameSystem[]).map(value =>
        <button key={value} className={system === value ? 'tab active' : 'tab'}
          onClick={() => setSystem(value)}>{SYSTEM_LABELS[value]}</button>)}
    </div>
    {error && <div className="error">{error}</div>}
    {loaded?.system === system
      ? <CustomTab key={system} system={system} reference={loaded.reference} onError={setError} refresh={refresh} />
      : <p>{t('Загрузка…', 'Loading…')}</p>}
  </div>
}
