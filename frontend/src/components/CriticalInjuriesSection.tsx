import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, RuleTableEntry } from '../api/types'
import { t } from '../i18n'

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

  // Группировка строк таблицы по тяжести для optgroup'ов (подпись — на языке интерфейса).
  const groups = useMemo(() => {
    const map = new Map<string, RuleTableEntry[]>()
    for (const e of table) {
      const g = (e.groupRu ? t(e.groupRu, e.groupEn || e.groupRu) : t('Прочее', 'Other'))
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
    if (isManual && !manualName.trim()) { onError(t('Укажите название ранения.', 'Enter the injury name.')); return }
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
      onError(err instanceof Error ? err.message : t('Ошибка добавления ранения', 'Failed to add injury'))
    } finally {
      setBusy(false)
    }
  }

  async function remove(id: string, name: string) {
    if (!confirm(t(`Снять крит-ранение «${name}»?`, `Remove critical injury "${name}"?`))) return
    try {
      await api.removeCriticalInjury(sheet.id, id)
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка снятия ранения', 'Failed to remove injury'))
    }
  }

  const injuries = sheet.criticalInjuries

  return (
    <section className="panel">
      <h3>{t('Критические ранения', 'Critical injuries')} {injuries.length > 0 ? `(${injuries.length})` : ''}</h3>
      <p className="muted small-text">
        {t('Сохраняются до лечения; каждое добавляет +10 к будущим броскам критических ранений (см. справочник правил).', 'They persist until healed; each adds +10 to future critical injury rolls (see the rules reference).')}
      </p>

      {injuries.length === 0 && <p className="muted">{t('Ранений нет.', 'No injuries.')}</p>}
      <div className="crit-list">
        {injuries.map(ci => (
          <div key={ci.id} className="crit-item">
            <div className="crit-item-main">
              {ci.severity && <span className="badge tier">{ci.severity}</span>}
              <strong>{ci.nameRu}</strong>
              {ci.rollResult != null && <span className="muted small-text"> · {t('бросок', 'roll')} {ci.rollResult}</span>}
              {ci.notes && <div className="muted small-text">{ci.notes}</div>}
            </div>
            <button className="danger small" onClick={() => void remove(ci.id, ci.nameRu)}>{t('Снять', 'Remove')}</button>
          </div>
        ))}
      </div>

      <form className="crit-form" onSubmit={submit}>
        <select value={code} onChange={e => setCode(e.target.value)} aria-label={t('Крит-ранение', 'Critical injury')}>
          <option value="" disabled>{t('— выберите ранение —', '— pick an injury —')}</option>
          {groups.map(([group, entries]) => (
            <optgroup key={group} label={group}>
              {entries.map(e => (
                <option key={e.code} value={e.code}>{e.rollRange} · {t(e.nameRu, e.nameEn || e.nameRu)}</option>
              ))}
            </optgroup>
          ))}
          <option value={MANUAL}>{t('— вручную —', '— manual —')}</option>
        </select>
        {code === MANUAL && (
          <input value={manualName} onChange={e => setManualName(e.target.value)}
            maxLength={200} placeholder={t('Название ранения', 'Injury name')} aria-label={t('Название ранения', 'Injury name')} />
        )}
        <input className="crit-roll" value={roll} onChange={e => setRoll(e.target.value)}
          inputMode="numeric" maxLength={3} placeholder="d100" aria-label={t('Бросок d100', 'd100 roll')} title={t('Результат броска (необязательно)', 'Roll result (optional)')} />
        <input className="grow" value={notes} onChange={e => setNotes(e.target.value)}
          maxLength={1000} placeholder={t('Заметки (необязательно)', 'Notes (optional)')} aria-label={t('Заметки', 'Notes')} />
        <button className="primary small" type="submit" disabled={busy || !code}>{t('Добавить', 'Add')}</button>
      </form>
      {selected?.body && <p className="hint small-text">{t(selected.body, selected.bodyEn || selected.body)}</p>}
    </section>
  )
}
