import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, Reference } from '../api/types'
import { SYSTEM_LABELS } from '../utils/labels'
import { SheetTab } from '../components/SheetTab'
import { TalentsTab } from '../components/TalentsTab'
import { InventoryTab } from '../components/InventoryTab'
import { CustomTab } from '../components/CustomTab'
import { NotesTab } from '../components/NotesTab'
import { MagicTab } from '../components/MagicTab'
import { PrintPreview } from '../components/print/PrintPreview'
import { CharacterSheetPrint } from '../components/print/CharacterSheetPrint'

interface Props {
  characterId: string
  onBack: () => void
}

type Tab = 'sheet' | 'talents' | 'inventory' | 'magic' | 'notes' | 'custom'

export function SheetPage({ characterId, onBack }: Props) {
  const [sheet, setSheet] = useState<CharacterSheet | null>(null)
  const [reference, setReference] = useState<Reference | null>(null)
  const [tab, setTab] = useState<Tab>('sheet')
  const [error, setError] = useState<string | null>(null)
  const [xpEdit, setXpEdit] = useState<string | null>(null)
  const [printing, setPrinting] = useState(false)

  const refresh = useCallback(
    () => api.sheet(characterId).then(next =>
      api.reference(next.system).then(ref => {
        setSheet(next)
        setReference(ref)
      })),
    [characterId])

  useEffect(() => {
    refresh().catch((err: unknown) => setError(err instanceof Error ? err.message : 'Ошибка загрузки'))
  }, [refresh])

  // Ошибка действия показывается и сама скрывается
  useEffect(() => {
    if (!error) return
    const timer = setTimeout(() => setError(null), 6000)
    return () => clearTimeout(timer)
  }, [error])

  if (!sheet || !reference) {
    return (
      <div className="page">
        <button onClick={onBack}>← Назад</button>
        {error ? <div className="error">{error}</div> : <p className="muted">Загрузка…</p>}
      </div>
    )
  }

  async function exportJson() {
    if (!sheet) return
    try {
      const data = await api.exportCharacter(sheet.id)
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${sheet.name.replace(/[^\p{L}\p{N}_-]+/gu, '_') || 'character'}.genesysforge.json`
      a.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка экспорта')
    }
  }

  async function saveXp() {
    if (xpEdit === null || !sheet) return
    const value = Number(xpEdit)
    setXpEdit(null)
    if (!Number.isFinite(value) || value === sheet.totalXp) return
    try {
      await api.updateCharacter(sheet.id, { totalXp: Math.trunc(value) })
      await refresh()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка')
    }
  }

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <button onClick={onBack}>← Персонажи</button>
          <h2 className="inline-title">{sheet.name}</h2>
          <span className={`badge ${sheet.system}`}>{SYSTEM_LABELS[sheet.system]}</span>
          <span className="muted"> {sheet.archetype.name} · {sheet.career.name}</span>
        </div>
        <div className="sheet-head-controls">
          <div className="xp-block">
            <span title="Суммарный опыт — кликните, чтобы изменить (награды ГМа)">
              XP: {xpEdit !== null ? (
                <input autoFocus className="xp-input" value={xpEdit}
                  onChange={e => setXpEdit(e.target.value)}
                  onBlur={() => void saveXp()}
                  onKeyDown={e => e.key === 'Enter' && void saveXp()} />
              ) : (
                <button className="linklike" onClick={() => setXpEdit(String(sheet.totalXp))}>{sheet.totalXp}</button>
              )}
            </span>
            <span className="muted"> потрачено {sheet.spentXp} · </span>
            <strong className="xp-available">доступно {sheet.availableXp}</strong>
          </div>
          <div className="sheet-action-buttons">
            <button className="small" title="Печать листа персонажа / сохранение в PDF"
              onClick={() => setPrinting(true)}>
              Печать листа
            </button>
            <button className="small" title="Скачать персонажа в JSON (бэкап / перенос между аккаунтами)"
              onClick={() => void exportJson()}>
              Экспорт JSON
            </button>
            {sheet.isCreationPhase && (
              <button className="small" title="Завершить создание: зафиксировать характеристики и снять лимит рангов"
                onClick={async () => { await api.completeCreation(sheet.id); await refresh() }}>
                Завершить создание
              </button>
            )}
          </div>
        </div>
      </div>

      {error && <div className="error floating">{error}</div>}

      <div className="tabs main-tabs">
        <button className={tab === 'sheet' ? 'tab active' : 'tab'} onClick={() => setTab('sheet')}>Лист</button>
        <button className={tab === 'talents' ? 'tab active' : 'tab'} onClick={() => setTab('talents')}>Таланты</button>
        <button className={tab === 'inventory' ? 'tab active' : 'tab'} onClick={() => setTab('inventory')}>Инвентарь</button>
        <button className={tab === 'magic' ? 'tab active' : 'tab'} onClick={() => setTab('magic')}>Магия</button>
        <button className={tab === 'notes' ? 'tab active' : 'tab'} onClick={() => setTab('notes')}>Заметки</button>
        <button className={tab === 'custom' ? 'tab active' : 'tab'} onClick={() => setTab('custom')}>Кастом</button>
      </div>

      {tab === 'sheet' && <SheetTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
      {tab === 'talents' && <TalentsTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
      {tab === 'inventory' && <InventoryTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
      {tab === 'magic' && <MagicTab sheet={sheet} onError={setError} />}
      {tab === 'notes' && <NotesTab characterId={sheet.id} onError={setError} />}
      {tab === 'custom' && <CustomTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}

      {printing && (
        <PrintPreview title={`Лист персонажа — ${sheet.name}`} onClose={() => setPrinting(false)}>
          {() => <CharacterSheetPrint sheet={sheet} reference={reference} />}
        </PrintPreview>
      )}
    </div>
  )
}
