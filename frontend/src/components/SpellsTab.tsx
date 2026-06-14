import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { GameSystem, Spell } from '../api/types'
import { magicSkillLabel } from '../utils/labels'

interface Props {
  system: GameSystem
  onError: (message: string) => void
}

/**
 * Справочник магии: переключение направлений (магических навыков) через dropdown,
 * отдельные таблицы базовых эффектов и дополнительных эффектов-модификаторов.
 * Состав навыков различается между Genesys Core и Realms of Terrinoth (Runes/Verse).
 */
export function SpellsTab({ system, onError }: Props) {
  const [spells, setSpells] = useState<Spell[] | null>(null)
  const [skill, setSkill] = useState<string>('')

  const reload = useCallback(
    () => api.spells(system)
      .then(setSpells)
      .catch((err: unknown) => onError(err instanceof Error ? err.message : 'Ошибка загрузки магии')),
    [system, onError])

  useEffect(() => { void reload() }, [reload])

  // Уникальные направления в порядке прихода с сервера.
  const skills = useMemo(() => {
    if (!spells) return []
    return [...new Set(spells.map(s => s.magicSkill))]
  }, [spells])

  // Активное направление: выбранное пользователем либо первое доступное
  // (без хранения дефолта в эффекте — корректно переживает смену системы).
  const activeSkill = skill && skills.includes(skill) ? skill : (skills[0] ?? '')

  const effects = useMemo(
    () => spells?.filter(s => s.magicSkill === activeSkill && s.kind === 'effect') ?? [],
    [spells, activeSkill])
  const modifiers = useMemo(
    () => spells?.filter(s => s.magicSkill === activeSkill && s.kind === 'additionalEffect') ?? [],
    [spells, activeSkill])

  if (spells === null) return <p className="muted">Загрузка…</p>

  return (
    <div>
      <section className="panel">
        <div className="spells-head">
          <h3>Магия</h3>
          <label className="inline-label">Направление
            <select value={activeSkill} onChange={e => setSkill(e.target.value)}>
              {skills.map(s => <option key={s} value={s}>{magicSkillLabel(s)}</option>)}
            </select>
          </label>
        </div>
        <p className="muted small-text">
          Заклинание = базовый эффект + при желании дополнительные эффекты, каждый из которых повышает сложность проверки.
        </p>
      </section>

      <section className="panel">
        <h3>Базовые эффекты {effects.length ? `(${effects.length})` : ''}</h3>
        <SpellTable rows={effects} />
      </section>

      <section className="panel">
        <h3>Дополнительные эффекты {modifiers.length ? `(${modifiers.length})` : ''}</h3>
        <SpellTable rows={modifiers} diffLabel="Сложность (+)" />
      </section>
    </div>
  )
}

function SpellTable({ rows, diffLabel = 'Сложность' }: { rows: Spell[]; diffLabel?: string }) {
  if (rows.length === 0) return <p className="muted">Нет записей для этого направления.</p>
  return (
    <div className="table-wrap">
      <table className="skills">
        <thead>
          <tr>
            <th>Название</th>
            <th>{diffLabel}</th>
            <th>Описание</th>
            <th>Источник</th>
          </tr>
        </thead>
        <tbody>
          {rows.map(s => (
            <tr key={s.id}>
              <td>
                <strong>{s.nameRu}</strong>
                <div className="muted small-text">{s.nameEn}{s.isCustom && ' · кастом'}</div>
              </td>
              <td className="nowrap">{s.difficulty}</td>
              <td>{s.description || s.safeDescription}</td>
              <td className="muted small-text">{s.source}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
