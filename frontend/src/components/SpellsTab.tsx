import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { GameSystem, Spell } from '../api/types'
import { magicSkillLabel } from '../utils/labels'

interface Props {
  system: GameSystem
  onError: (message: string) => void
}

/**
 * Справочник магии. Сначала выбирается магический навык (направление) — от него зависит
 * список доступных базовых эффектов. Затем выбирается базовый эффект по названию: для него
 * показываются описание и таблица дополнительных эффектов, привязанных именно к нему.
 */
export function SpellsTab({ system, onError }: Props) {
  const [spells, setSpells] = useState<Spell[] | null>(null)
  const [skill, setSkill] = useState<string>('')
  const [effectCode, setEffectCode] = useState<string>('')

  const reload = useCallback(
    () => api.spells(system)
      .then(setSpells)
      .catch((err: unknown) => onError(err instanceof Error ? err.message : 'Ошибка загрузки магии')),
    [system, onError])

  useEffect(() => { void reload() }, [reload])

  // Навыки — только из базовых эффектов (у доп. эффектов навык не задан).
  const skills = useMemo(() => {
    if (!spells) return []
    return [...new Set(spells.filter(s => s.kind === 'effect').map(s => s.magicSkill))]
  }, [spells])

  // Активный навык: выбранный либо первый доступный (без хранения дефолта в эффекте).
  const activeSkill = skill && skills.includes(skill) ? skill : (skills[0] ?? '')

  // Базовые эффекты, доступные выбранному навыку.
  const baseEffects = useMemo(
    () => spells?.filter(s => s.kind === 'effect' && s.magicSkill === activeSkill) ?? [],
    [spells, activeSkill])

  // Активный базовый эффект (по стабильному коду nameEn), скорректированный под доступные.
  const activeEffectCode = effectCode && baseEffects.some(e => e.nameEn === effectCode)
    ? effectCode
    : (baseEffects[0]?.nameEn ?? '')

  const selectedEffect = baseEffects.find(e => e.nameEn === activeEffectCode) ?? null

  // Дополнительные эффекты, привязанные к выбранному базовому.
  const additional = useMemo(
    () => spells?.filter(s => s.kind === 'additionalEffect' && s.parentEffect === activeEffectCode) ?? [],
    [spells, activeEffectCode])

  if (spells === null) return <p className="muted">Загрузка…</p>

  return (
    <div>
      <section className="panel">
        <div className="spells-head">
          <h3>Магия</h3>
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
        <p className="muted small-text">
          Заклинание = базовый эффект выбранного направления + при желании дополнительные эффекты,
          каждый из которых повышает сложность проверки.
        </p>
      </section>

      {selectedEffect && (
        <section className="panel">
          <div className="spell-detail-head">
            <h3>{selectedEffect.nameRu} <span className="muted">· {selectedEffect.nameEn}</span></h3>
            <span className="difficulty-badge">Сложность: {selectedEffect.difficulty}</span>
          </div>
          <p>{selectedEffect.description || selectedEffect.safeDescription}</p>
          <div className="muted small-text">Источник: {selectedEffect.source}</div>
        </section>
      )}

      <section className="panel">
        <h3>Дополнительные эффекты {additional.length ? `(${additional.length})` : ''}</h3>
        {additional.length === 0
          ? <p className="muted">У этого базового эффекта нет дополнительных эффектов.</p>
          : (
            <div className="table-wrap">
              <table className="skills">
                <thead>
                  <tr>
                    <th>Название</th>
                    <th>Сложность (+)</th>
                    <th>Описание</th>
                    <th>Источник</th>
                  </tr>
                </thead>
                <tbody>
                  {additional.map(m => (
                    <tr key={m.id}>
                      <td>
                        <strong>{m.nameRu}</strong>
                        <div className="muted small-text">{m.nameEn}{m.isCustom && ' · кастом'}</div>
                      </td>
                      <td className="nowrap">{m.difficulty}</td>
                      <td>{m.description || m.safeDescription}</td>
                      <td className="muted small-text">{m.source}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </section>
    </div>
  )
}
