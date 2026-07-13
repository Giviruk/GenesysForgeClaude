import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { GameSystem, Spell } from '../api/types'
import { localizedDescription, localizedName, magicSkillLabel } from '../utils/labels'
import { t } from '../i18n'

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
      .catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка загрузки магии', 'Failed to load magic'))),
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

  if (spells === null) return <p className="muted">{t('Загрузка…', 'Loading…')}</p>

  return (
    <div>
      <section className="panel">
        <div className="spells-head">
          <h3>{t('Магия', 'Magic')}</h3>
          <div className="spells-selectors">
            <label className="inline-label">{t('Направление', 'School')}
              <select value={activeSkill} onChange={e => setSkill(e.target.value)}>
                {skills.map(s => <option key={s} value={s}>{magicSkillLabel(s)}</option>)}
              </select>
            </label>
            <label className="inline-label">{t('Базовый эффект', 'Base effect')}
              <select value={activeEffectCode} onChange={e => setEffectCode(e.target.value)}>
                {baseEffects.map(e => <option key={e.id} value={e.nameEn}>{localizedName({ name: e.nameEn, nameRu: e.nameRu })}</option>)}
              </select>
            </label>
          </div>
        </div>
        <p className="muted small-text">
          {t(
            'Заклинание = базовый эффект выбранного направления + при желании дополнительные эффекты, ' +
            'каждый из которых повышает сложность проверки.',
            'A spell = the base effect of the chosen school, plus optional additional effects, ' +
            'each of which increases the check difficulty.',
          )}
        </p>
      </section>

      {selectedEffect && (
        <section className="panel">
          <div className="spell-detail-head">
            <h3>{t(selectedEffect.nameRu, selectedEffect.nameEn)} <span className="muted">· {t(selectedEffect.nameEn, selectedEffect.nameRu)}</span></h3>
            <span className="difficulty-badge">{t('Сложность:', 'Difficulty:')} {selectedEffect.difficulty}</span>
          </div>
          <p>{localizedDescription(selectedEffect)}</p>
          <div className="muted small-text">{t('Источник:', 'Source:')} {selectedEffect.source}</div>
        </section>
      )}

      <section className="panel">
        <h3>{t('Дополнительные эффекты', 'Additional effects')} {additional.length ? `(${additional.length})` : ''}</h3>
        {additional.length === 0
          ? <p className="muted">{t('У этого базового эффекта нет дополнительных эффектов.', 'This base effect has no additional effects.')}</p>
          : (
            <div className="table-wrap">
              <table className="skills">
                <thead>
                  <tr>
                    <th>{t('Название', 'Name')}</th>
                    <th>{t('Сложность (+)', 'Difficulty (+)')}</th>
                    <th>{t('Описание', 'Description')}</th>
                    <th>{t('Источник', 'Source')}</th>
                  </tr>
                </thead>
                <tbody>
                  {additional.map(m => (
                    <tr key={m.id}>
                      <td>
                        <strong>{t(m.nameRu, m.nameEn)}</strong>
                        <div className="muted small-text">{t(m.nameEn, m.nameRu)}{m.isCustom && t(' · кастом', ' · custom')}</div>
                      </td>
                      <td className="nowrap">{m.difficulty}</td>
                      <td>{localizedDescription(m)}</td>
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
