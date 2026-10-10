import { magicActions } from '../utils/magicActions'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { GameSystem, Quality, Spell } from '../api/types'
import { difficultyLabel, localizedDescription, magicSkillLabel } from '../utils/labels'
import { t } from '../i18n'
import { PropertyText } from './PropertyText'
import { MagicDirectionChips } from './MagicDirectionChips'
import type { MagicSkillPool } from './MagicBuilder'
import { BookReference } from './BookReference'

interface Props {
  characterId?: string; campaignId?: string; system: GameSystem; onError: (message: string) => void; qualities?: Quality[]
  direction?: string; onDirectionChange?: (skill: string) => void; action?: string; onActionChange?: (action: string) => void
  modeControl?: ReactNode; characterSkills?: MagicSkillPool[]
}
export function SpellsTab({ system, characterId, campaignId, onError, qualities, direction, onDirectionChange, action, onActionChange, modeControl, characterSkills }: Props) {
  const [spells, setSpells] = useState<Spell[] | null>(null)
  const [localSkill, setLocalSkill] = useState('')
  const [localEffect, setLocalEffect] = useState('')
  const skill = direction ?? localSkill
  const effectCode = action ?? localEffect
  const setSkill = (value: string) => { setLocalSkill(value); onDirectionChange?.(value) }
  const setEffectCode = (value: string) => { setLocalEffect(value); onActionChange?.(value) }
  const reload = useCallback(() => api.spells(system, campaignId ? { campaignId } : characterId ? { characterId } : undefined)
    .then(setSpells).catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка загрузки магии', 'Failed to load magic'))), [system, characterId, campaignId, onError])
  useEffect(() => { void reload() }, [reload])
  const skills = useMemo(() => [...new Set(spells?.filter(s => s.kind === 'effect').map(s => s.magicSkill) ?? [])], [spells])
  const activeSkill = skills.includes(skill) ? skill : skills[0] ?? ''
  const baseEffects = spells?.filter(s => s.kind === 'effect' && s.magicSkill === activeSkill) ?? []
  const activeEffectCode = baseEffects.some(e => e.nameEn === effectCode) ? effectCode : baseEffects[0]?.nameEn ?? ''
  const selected = baseEffects.find(e => e.nameEn === activeEffectCode)
  const additional = spells?.filter(s => s.kind === 'additionalEffect' && s.parentEffect === activeEffectCode) ?? []
  const matrix = magicActions(spells ?? [], activeSkill)
  function selectAction(code: string) {
    if (!baseEffects.some(e => e.nameEn === code)) {
      const available = spells?.find(s => s.kind === 'effect' && s.nameEn === code)
      if (available) setSkill(available.magicSkill)
    }
    setEffectCode(code)
  }
  if (spells === null) return <p className="muted">{t('Загрузка…', 'Loading…')}</p>
  return <div className="magic-reference">
    <div className="sheet-mode-header">{modeControl}<MagicDirectionChips skills={skills} activeSkill={activeSkill} onChange={setSkill} characterSkills={characterSkills} /></div>
    <div className="magic-reference-layout sheet-two-column">
      <section className="panel magic-matrix"><h3>{t('Действия по направлениям', 'Actions by school')}</h3>
        <div className="table-wrap"><table className="skills"><thead><tr><th>{t('Действие', 'Action')}</th>{skills.map(s => <th className={s === activeSkill ? 'magic-active-column' : ''} key={s}>{magicSkillLabel(s)}</th>)}</tr></thead>
          <tbody>{matrix.map(a => <tr key={a.nameEn} className={a.nameEn === activeEffectCode ? 'active-row' : ''} onClick={() => selectAction(a.nameEn)}>
            <td><button className="magic-matrix-action" aria-pressed={a.nameEn === activeEffectCode} onClick={e => { e.stopPropagation(); selectAction(a.nameEn) }}><strong>{t(a.nameRu, a.nameEn)}</strong><small>{difficultyLabel(a.difficultyIncrease)} ({a.difficultyIncrease}){a.isOptional && ' · EPG'}</small></button></td>
            {skills.map(s => <td key={s} className={s === activeSkill ? 'magic-active-column' : ''} aria-label={a.allowedSkills.includes(s) ? t(`${magicSkillLabel(s)}: доступно`, `${magicSkillLabel(s)}: available`) : t(`${magicSkillLabel(s)}: недоступно`, `${magicSkillLabel(s)}: unavailable`)}>{a.allowedSkills.includes(s) ? '✓' : <span className="muted">—</span>}</td>)}
          </tr>)}</tbody></table></div>
        <p className="muted small-text">{t('Прочерк — направление такого действия не умеет; это правило книги, а не пропуск в справочнике.', 'A dash means the school cannot perform that action — that is the rule, not a gap in the reference.')}</p>
      </section>
      {selected && <aside className="sheet-result-card sheet-sticky magic-reference-detail">
        <div className="sheet-card-heading"><h3>{t(selected.nameRu, selected.nameEn)}</h3><span className="difficulty-badge">{t('Сложность:', 'Difficulty:')} {difficultyLabel(selected.difficultyIncrease)} ({selected.difficultyIncrease})</span></div>
        <p><PropertyText text={localizedDescription(selected)} qualities={qualities} /></p><p className="muted small-text">{t('Доступно направлениям:', 'Available to:')} {selected.allowedSkills.map(magicSkillLabel).join(', ')}{selected.isOptional && t(' · опциональные правила (EPG)', ' · optional rules (EPG)')}</p><BookReference source={selected.source} />
        <h4>{t('Дополнительные эффекты', 'Additional effects')} · {additional.length}</h4>
        {!additional.length && <p className="muted">{t('У этого базового эффекта нет дополнительных эффектов.', 'This base effect has no additional effects.')}</p>}
        {additional.map(a => <article className="magic-reference-effect" key={a.id}><div className="sheet-card-heading"><strong>{t(a.nameRu, a.nameEn)}</strong><span className="accent-text">{a.difficulty}</span></div>
          <p className="small-text"><PropertyText text={localizedDescription(a)} qualities={qualities} /></p><p className="muted small-text">{a.allowedSkills.length > 0 && <>{t('только', 'only')} {a.allowedSkills.map(magicSkillLabel).join(', ')}</>}
            {a.exclusions.length > 0 && <span> · {t('не сочетается с', 'not combinable with')} {a.exclusions.map(code => t(additional.find(x => x.nameEn === code)?.nameRu || code, code)).join(', ')}</span>}
            {a.repeatable && <span> · {t('можно добавлять несколько раз', 'can be added several times')}</span>}
            {a.usesKnowledgeRating && <span> · {t('рейтинг = ранги Знания', 'rating = Knowledge ranks')}</span>}{a.isCustom && <span> · {t('кастом', 'custom')}</span>}</p><BookReference source={a.source} />
        </article>)}
      </aside>}
    </div>
  </div>
}
