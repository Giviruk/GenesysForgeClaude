import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet } from '../api/types'
import { lang, t } from '../i18n'
import { wordCountLabel } from '../utils/wordCountLabel'
import { localizedName } from '../utils/labels'
import { Icon, type IconName } from './Icon'
import { NotesTab } from './NotesTab'

interface Props {
  sheet: CharacterSheet
  onError?: (message: string) => void
  refresh?: () => Promise<void>
  onPortraitClick?: () => void
  readOnly?: boolean
}

const MOTIVATIONS = [
  { key: 'desire', label: t('Стремление', 'Desire'), icon: 'target', hint: t('Чего персонаж хочет добиться, к чему стремится', 'What the character wants to achieve or strives for') },
  { key: 'fear', label: t('Страх', 'Fear'), icon: 'ghost-2', hint: t('Чего персонаж боится или избегает', 'What the character fears or avoids') },
  { key: 'strength', label: t('Сильная сторона', 'Strength'), icon: 'shield-check', hint: t('Положительная черта характера', 'A positive character trait') },
  { key: 'flaw', label: t('Слабость', 'Flaw'), icon: 'heart-broken', hint: t('Недостаток или порок', 'A shortcoming or vice') },
] as const
const bioValues = (sheet: CharacterSheet) => ({ desire: sheet.desire ?? '', fear: sheet.fear ?? '', strength: sheet.strength ?? '', flaw: sheet.flaw ?? '', background: sheet.background ?? '' })

export function BioTab({ sheet, onError, refresh, onPortraitClick, readOnly = false }: Props) {
  const [values, setValues] = useState(() => bioValues(sheet))
  const [baseline, setBaseline] = useState(() => bioValues(sheet))
  const [busy, setBusy] = useState(false)
  const [saved, setSaved] = useState(false)
  const shown = readOnly ? bioValues(sheet) : values
  const dirty = JSON.stringify(values) !== JSON.stringify(baseline)
  const filled = Object.values(shown).filter(v => v.trim()).length
  const words = shown.background.trim() ? shown.background.trim().split(/\s+/).length : 0
  function change(key: keyof typeof values, value: string) { setSaved(false); setValues(prev => ({ ...prev, [key]: value })) }
  async function submit(e: FormEvent) {
    e.preventDefault()
    if (readOnly || busy || !dirty) return
    setBusy(true)
    const snapshot = { ...values }
    try {
      await api.updateCharacter(sheet.id, snapshot)
      setBaseline(snapshot)
      await refresh?.()
      setSaved(true)
    } catch (err) { onError?.(err instanceof Error ? err.message : t('Ошибка сохранения', 'Failed to save')) }
    finally { setBusy(false) }
  }
  const portrait = <>{sheet.portraitUrl ? <img src={sheet.portraitUrl} alt={t(`Портрет: ${sheet.name}`, `Portrait: ${sheet.name}`)} />
    : <><Icon name="photo-plus" /><small>{t('портрет', 'portrait')}</small></>}</>
  return <div className="bio-layout sheet-two-column">
    <form onSubmit={submit} className="bio-main">
      <section className="panel bio-identity">
        {readOnly ? <div className="bio-portrait">{portrait}</div> : <button type="button" className="bio-portrait" onClick={onPortraitClick}
          title={t('Загрузить портрет (JPEG/PNG/WebP, до 5 МБ)', 'Upload portrait (JPEG/PNG/WebP, up to 5 MB)')}>{portrait}</button>}
        <div><h3>{sheet.name}</h3><p className="muted small-text">{sheet.archetype && localizedName(sheet.archetype)} · {sheet.career && localizedName(sheet.career)}</p>
          <div className="bio-progress"><div className="bio-progress-track"><i style={{ width: `${filled * 20}%`, background: filled === 5 ? 'var(--green)' : 'var(--accent)' }} /></div>
            <small>{filled === 5 ? t('Образ заполнен', 'Bio complete') : t(`Заполнено ${filled} из 5`, `Completed ${filled} of 5`)}</small></div>
        </div>
      </section>
      <h3 className="sheet-section-title">{t('Мотивации', 'Motivations')}</h3>
      <div className="bio-motivations">{MOTIVATIONS.map(m => <label key={m.key} className={`bio-motivation ${m.key}`}>
        <span><Icon name={m.icon as IconName} />{m.label}</span>
        <textarea aria-label={m.label} value={shown[m.key]} readOnly={readOnly} disabled={busy} onChange={e => change(m.key, e.target.value)} rows={2} maxLength={300} placeholder={m.hint} />
      </label>)}</div>
      <h3 className="sheet-section-title">{t('Предыстория', 'Background')}<small>{wordCountLabel(words, lang)}</small></h3>
      <textarea className="bio-background" aria-label={t('Предыстория', 'Background')} value={shown.background} readOnly={readOnly} disabled={busy}
        onChange={e => change('background', e.target.value)} rows={10} maxLength={8000}
        placeholder={t('История персонажа: происхождение, важные события, связи, цели…', 'The character’s story: origin, key events, connections, goals…')} />
      {!readOnly && <div className="form-actions bio-save"><button className="primary" type="submit" disabled={busy || !dirty}>{t('Сохранить', 'Save')}</button>
        <span role="status" className={dirty ? 'warn-text small-text' : 'muted small-text'}><Icon name={dirty ? 'point-filled' : 'check'} className="button-icon" />
          {dirty ? t('Есть несохранённые изменения', 'Unsaved changes') : saved ? t('Сохранено', 'Saved') : t('Все изменения сохранены', 'All changes saved')}</span></div>}
    </form>
    {!readOnly && onError && <aside className="sheet-sticky"><NotesTab key={sheet.id} characterId={sheet.id} onError={onError} /></aside>}
  </div>
}
