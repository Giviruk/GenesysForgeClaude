import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, RuleTableEntry } from '../api/types'

interface Props {
  sheet: CharacterSheet
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const MANUAL = '__manual__'

/** Секция критических ранений на листе (U-23). Выбор из таблицы U-11 или ручной ввод. */
export function CriticalInjuriesSection({ sheet, onError, refresh }: Props) {
  const [table, setTable] = useState<RuleTableEntry[]>([])
  const [code, setCode] = useState('')
  const [manualName, setManualName] = useState('')
  const [roll, setRoll] = useState('')
  const [notes, setNotes] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api.rules()
      .then(r => setTable(r.entries.filter(e => e.kind === 'criticalInjury' && e.rollRange)))
      .catch(() => { /* таблица не критична для отображения списка */ })
  }, [])

  // Группировка строк таблицы по тяжести для optgroup'ов.
  const groups = useMemo(() => {
    const map = new Map<string, RuleTableEntry[]>()
    for (const e of table) {
      const g = e.groupRu || 'Прочее'
      const list = map.get(g) ?? []
      list.push(e)
      map.set(g, list)
    }
    return [...map.entries()]
  }, [table])

  const selected = table.find(e => e.code === code)

  async function submit(e: FormEvent) {
    e.preventDefault()
    const isManual = code === MANUAL
    if (!code) return
    if (isManual && !manualName.trim()) { onError('Укажите название ранения.'); return }
    setBusy(true)
    try {
      const rollNum = Number(roll)
      await api.addCriticalInjury(sheet.id, {
        ruleCode: isManual ? undefined : code,
        nameRu: isManual ? manualName.trim() : undefined,
        rollResult: roll.trim() && Number.isFinite(rollNum) ? Math.trunc(rollNum) : undefined,
        notes: notes.trim() || undefined,
      })
      setCode(''); setManualName(''); setRoll(''); setNotes('')
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка добавления ранения')
    } finally {
      setBusy(false)
    }
  }

  async function remove(id: string, name: string) {
    if (!confirm(`Снять крит-ранение «${name}»?`)) return
    try {
      await api.removeCriticalInjury(sheet.id, id)
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка снятия ранения')
    }
  }

  const injuries = sheet.criticalInjuries

  return (
    <section className="panel">
      <h3>Критические ранения {injuries.length > 0 ? `(${injuries.length})` : ''}</h3>
      <p className="muted small-text">
        Сохраняются до лечения; каждое добавляет +10 к будущим броскам критических ранений (см. справочник правил).
      </p>

      {injuries.length === 0 && <p className="muted">Ранений нет.</p>}
      <div className="crit-list">
        {injuries.map(ci => (
          <div key={ci.id} className="crit-item">
            <div className="crit-item-main">
              {ci.severity && <span className="badge tier">{ci.severity}</span>}
              <strong>{ci.nameRu}</strong>
              {ci.rollResult != null && <span className="muted small-text"> · бросок {ci.rollResult}</span>}
              {ci.notes && <div className="muted small-text">{ci.notes}</div>}
            </div>
            <button className="danger small" onClick={() => void remove(ci.id, ci.nameRu)}>Снять</button>
          </div>
        ))}
      </div>

      <form className="crit-form" onSubmit={submit}>
        <select value={code} onChange={e => setCode(e.target.value)} aria-label="Крит-ранение">
          <option value="" disabled>— выберите ранение —</option>
          {groups.map(([group, entries]) => (
            <optgroup key={group} label={group}>
              {entries.map(e => (
                <option key={e.code} value={e.code}>{e.rollRange} · {e.nameRu}</option>
              ))}
            </optgroup>
          ))}
          <option value={MANUAL}>— вручную —</option>
        </select>
        {code === MANUAL && (
          <input value={manualName} onChange={e => setManualName(e.target.value)}
            maxLength={200} placeholder="Название ранения" aria-label="Название ранения" />
        )}
        <input className="crit-roll" value={roll} onChange={e => setRoll(e.target.value)}
          inputMode="numeric" maxLength={3} placeholder="d100" aria-label="Бросок d100" title="Результат броска (необязательно)" />
        <input className="grow" value={notes} onChange={e => setNotes(e.target.value)}
          maxLength={1000} placeholder="Заметки (необязательно)" aria-label="Заметки" />
        <button className="primary small" type="submit" disabled={busy || !code}>Добавить</button>
      </form>
      {selected?.body && <p className="hint small-text">{selected.body}</p>}
    </section>
  )
}
