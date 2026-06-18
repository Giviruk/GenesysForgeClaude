import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { DicePool, GameSystem, Spell } from '../api/types'
import { difficultyLabel, magicSkillLabel, parseDifficulty } from '../utils/labels'
import { DicePoolView } from './DicePoolView'

export interface MagicSkillPool {
  name: string
  pool: DicePool
}

interface Props {
  system: GameSystem
  /** Магические навыки персонажа с пулами кубов — для интеграции с листом (необязательно). */
  characterSkills?: MagicSkillPool[]
  onError: (message: string) => void
}

/**
 * Magic Action Builder: пользователь выбирает направление магии и базовый эффект, отмечает
 * дополнительные эффекты, а сборщик считает итоговую сложность и собирает текст для копирования.
 * Работает поверх того же справочника, что и SpellsTab; персонажа знать не обязательно (режим GM).
 */
export function MagicBuilder({ system, characterSkills, onError }: Props) {
  const [spells, setSpells] = useState<Spell[] | null>(null)
  const [skill, setSkill] = useState('')
  const [effectCode, setEffectCode] = useState('')
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  const reload = useCallback(
    () => api.spells(system)
      .then(setSpells)
      .catch((err: unknown) => onError(err instanceof Error ? err.message : 'Ошибка загрузки магии')),
    [system, onError])
  useEffect(() => { void reload() }, [reload])

  const skills = useMemo(
    () => spells ? [...new Set(spells.filter(s => s.kind === 'effect').map(s => s.magicSkill))] : [],
    [spells])
  const activeSkill = skill && skills.includes(skill) ? skill : (skills[0] ?? '')

  const baseEffects = useMemo(
    () => spells?.filter(s => s.kind === 'effect' && s.magicSkill === activeSkill) ?? [],
    [spells, activeSkill])
  const activeEffectCode = effectCode && baseEffects.some(e => e.nameEn === effectCode)
    ? effectCode
    : (baseEffects[0]?.nameEn ?? '')
  const selectedEffect = baseEffects.find(e => e.nameEn === activeEffectCode) ?? null

  const additional = useMemo(
    () => spells?.filter(s => s.kind === 'additionalEffect' && s.parentEffect === activeEffectCode) ?? [],
    [spells, activeEffectCode])

  const chosen = additional.filter(a => selectedIds.has(a.id))
  const baseDifficulty = selectedEffect ? parseDifficulty(selectedEffect.difficulty) : 0
  const added = chosen.reduce((sum, a) => sum + parseDifficulty(a.difficulty), 0)
  const totalDifficulty = Math.min(5, baseDifficulty + added) // потолок сложности Genesys — 5

  // Пул кубов персонажа для выбранного направления (если передан лист).
  const charPool = characterSkills?.find(s => s.name === activeSkill)?.pool ?? null

  const toggle = (id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }

  const buildText = (): string => {
    if (!selectedEffect) return ''
    const lines = [
      `Магическое действие — ${magicSkillLabel(activeSkill)}`,
      `Базовый эффект: ${selectedEffect.nameRu} (${selectedEffect.nameEn})`,
      `Сложность: ${totalDifficulty} (${difficultyLabel(totalDifficulty)})`
        + (added > 0 ? ` — базовая ${baseDifficulty} + ${added}` : ''),
    ]
    if (chosen.length) {
      lines.push('Доп. эффекты:')
      for (const a of chosen) lines.push(`  • ${a.nameRu} (${a.nameEn}) ${a.difficulty} — ${a.safeDescription || a.description}`)
    }
    const sources = [...new Set([selectedEffect.source, ...chosen.map(a => a.source)].filter(Boolean))]
    if (sources.length) lines.push(`Источники: ${sources.join('; ')}`)
    return lines.join('\n')
  }

  if (spells === null) return <p className="muted">Загрузка…</p>
  if (skills.length === 0) return <p className="muted">Для этой системы нет магических направлений.</p>

  return (
    <div className="magic-builder">
      <section className="panel">
        <div className="spells-head">
          <h3>Сборка магического действия</h3>
          <div className="spells-selectors">
            <label className="inline-label">Направление
              <select value={activeSkill} onChange={e => setSkill(e.target.value)}>
                {skills.map(s => <option key={s} value={s}>{magicSkillLabel(s)}</option>)}
              </select>
            </label>
            <label className="inline-label">Базовый эффект
              <select value={activeEffectCode} onChange={e => setEffectCode(e.target.value)}>
                {baseEffects.map(e => <option key={e.id} value={e.nameEn}>{e.nameRu}</option>)}
              </select>
            </label>
          </div>
        </div>
        {charPool && (
          <div className="muted small-text">
            Ваш пул для «{magicSkillLabel(activeSkill)}»: <DicePoolView pool={charPool} />
          </div>
        )}
      </section>

      {selectedEffect && (
        <section className="panel magic-result">
          <div className="spell-detail-head">
            <h3>{selectedEffect.nameRu} <span className="muted">· {selectedEffect.nameEn}</span></h3>
            <span className="difficulty-badge big">
              Сложность: {totalDifficulty} · {difficultyLabel(totalDifficulty)}
            </span>
          </div>
          <div className="difficulty-dice" aria-label={`${totalDifficulty} кубов сложности`}>
            {Array.from({ length: totalDifficulty }).map((_, i) => <span key={i} className="die difficulty">▲</span>)}
            {totalDifficulty === 0 && <span className="muted">— (простая проверка)</span>}
          </div>
          {added > 0 && <div className="muted small-text">Базовая {baseDifficulty} + дополнительные {added}</div>}
          <p>{selectedEffect.description || selectedEffect.safeDescription}</p>
          <div className="muted small-text">Источник: {selectedEffect.source}</div>
          <div className="card-actions">
            <CopyButton key={buildText()} text={buildText()} onError={onError} />
          </div>
        </section>
      )}

      <section className="panel">
        <h3>Дополнительные эффекты {additional.length ? `(выбрано ${chosen.length} из ${additional.length})` : ''}</h3>
        <p className="hint">Каждый эффект повышает сложность; описание содержит траты преимуществ/угроз при активации.</p>
        {additional.length === 0
          ? <p className="muted">У этого базового эффекта нет дополнительных эффектов.</p>
          : (
            <div className="effect-list">
              {additional.map(a => {
                const on = selectedIds.has(a.id)
                return (
                  <label key={a.id} className={on ? 'effect-row on' : 'effect-row'}>
                    <input type="checkbox" checked={on} onChange={() => toggle(a.id)} />
                    <span className="effect-main">
                      <strong>{a.nameRu}</strong> <span className="muted small-text">{a.nameEn}</span>
                      <span className="difficulty-badge">{a.difficulty}</span>
                      <div className="small-text">{a.safeDescription || a.description}</div>
                    </span>
                  </label>
                )
              })}
            </div>
          )}
      </section>
    </div>
  )
}

/** Кнопка копирования карточки. Перемонтируется по key=text, поэтому «Скопировано ✓» само сбрасывается. */
function CopyButton({ text, onError }: { text: string; onError: (m: string) => void }) {
  const [copied, setCopied] = useState(false)
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(text)
      setCopied(true)
    } catch {
      onError('Не удалось скопировать в буфер обмена.')
    }
  }
  return <button className="primary small" onClick={copy}>{copied ? 'Скопировано ✓' : 'Скопировать карточку'}</button>
}
