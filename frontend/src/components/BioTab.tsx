import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet } from '../api/types'

interface Props {
  sheet: CharacterSheet
  onError: (message: string) => void
  refresh: () => Promise<void>
}

// Мотивации Genesys + свободная предыстория (U-22). Поля опциональны.
const MOTIVATIONS = [
  { key: 'desire', label: 'Стремление', hint: 'Чего персонаж хочет добиться, к чему стремится' },
  { key: 'fear', label: 'Страх', hint: 'Чего персонаж боится или избегает' },
  { key: 'strength', label: 'Сильная сторона', hint: 'Положительная черта характера' },
  { key: 'flaw', label: 'Слабость', hint: 'Недостаток или порок' },
] as const

export function BioTab({ sheet, onError, refresh }: Props) {
  const [desire, setDesire] = useState(sheet.desire ?? '')
  const [fear, setFear] = useState(sheet.fear ?? '')
  const [strength, setStrength] = useState(sheet.strength ?? '')
  const [flaw, setFlaw] = useState(sheet.flaw ?? '')
  const [background, setBackground] = useState(sheet.background ?? '')
  const [busy, setBusy] = useState(false)
  const [saved, setSaved] = useState(false)

  const values = { desire, fear, strength, flaw, background }
  const setters: Record<string, (v: string) => void> = { desire: setDesire, fear: setFear, strength: setStrength, flaw: setFlaw }

  const dirty =
    (sheet.desire ?? '') !== desire ||
    (sheet.fear ?? '') !== fear ||
    (sheet.strength ?? '') !== strength ||
    (sheet.flaw ?? '') !== flaw ||
    (sheet.background ?? '') !== background

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setSaved(false)
    try {
      await api.updateCharacter(sheet.id, { desire, fear, strength, flaw, background })
      await refresh()
      setSaved(true)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка сохранения')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={submit}>
      <section className="panel">
        <h3>Мотивации</h3>
        <p className="muted small-text">
          Стремления, страхи, сильные и слабые стороны персонажа. Помогают отыгрышу и решениям мастера.
        </p>
        <div className="custom-form">
          {MOTIVATIONS.map(m => (
            <label key={m.key}>{m.label}
              <input value={values[m.key]} onChange={e => { setSaved(false); setters[m.key](e.target.value) }}
                maxLength={300} placeholder={m.hint} />
            </label>
          ))}
        </div>
      </section>

      <section className="panel">
        <h3>Предыстория</h3>
        <textarea aria-label="Предыстория" value={background}
          onChange={e => { setSaved(false); setBackground(e.target.value) }}
          rows={10} maxLength={8000} placeholder="История персонажа: происхождение, важные события, связи, цели…" />
      </section>

      <div className="form-actions">
        <button className="primary" type="submit" disabled={busy || !dirty}>Сохранить</button>
        {saved && !dirty && <span className="muted small-text">Сохранено</span>}
      </div>
    </form>
  )
}
