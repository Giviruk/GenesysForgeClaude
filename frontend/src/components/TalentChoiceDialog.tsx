import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { GameSystem, SkillKind, Spell } from '../api/types'
import { lang, t } from '../i18n'
import { SKILL_KIND_LABELS } from '../utils/labels'
import {
  signatureSpellLabel, signatureSpellValue, spellNameResolver, type TalentChoiceOption,
} from '../utils/talentChoices'

interface ChoiceProps {
  title: string
  hint: string
  /** Сколько значений нужно выбрать для покупаемого ранга. */
  count: number
  options: TalentChoiceOption[]
  onConfirm: (values: string[]) => void
  onCancel: () => void
}

/**
 * Выбор характеристик или навыков при покупке ранга таланта (ROT-TAL-03): ровно `count`
 * разных значений из допустимых вариантов.
 */
export function TalentChoiceDialog({ title, hint, count, options, onConfirm, onCancel }: ChoiceProps) {
  const [selected, setSelected] = useState<string[]>([])

  const toggle = (value: string) => setSelected(prev => prev.includes(value)
    ? prev.filter(x => x !== value)
    // Одно значение — как переключатель: новый выбор заменяет прежний.
    : count === 1 ? [value]
    : prev.length >= count ? prev : [...prev, value])

  const groups = options.reduce<Array<{ group?: SkillKind; options: TalentChoiceOption[] }>>((acc, option) => {
    const last = acc[acc.length - 1]
    if (last && last.group === option.group) last.options.push(option)
    else acc.push({ group: option.group, options: [option] })
    return acc
  }, [])

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()}
        onSubmit={e => { e.preventDefault(); if (selected.length === count) onConfirm(selected) }}>
        <h3>{title}</h3>
        <p className="hint">{hint}</p>
        {options.length < count ? (
          <p className="muted">{t('Нет доступных вариантов для выбора.', 'No options are available.')}</p>
        ) : groups.map(g => (
          <div key={g.group ?? 'all'} className="talent-choice-group">
            {g.group && <div className="talent-group-title">{SKILL_KIND_LABELS[g.group]}</div>}
            <div className="chips">
              {g.options.map(option => {
                const on = selected.includes(option.value)
                return (
                  <button key={option.value} type="button" aria-pressed={on}
                    className={on ? 'chip active' : 'chip'}
                    disabled={!on && count > 1 && selected.length >= count}
                    onClick={() => toggle(option.value)}>
                    {option.label}
                    {option.note && <span className="muted"> {option.note}</span>}
                  </button>
                )
              })}
            </div>
          </div>
        ))}
        <div className="modal-actions">
          <span className="muted small-text talent-choice-count">{t(`Выбрано ${selected.length} из ${count}`, `Selected ${selected.length} of ${count}`)}</span>
          <button type="button" onClick={onCancel}>{t('Отмена', 'Cancel')}</button>
          <button type="submit" className="primary" disabled={selected.length !== count}>
            {t('Подтвердить и купить', 'Confirm and buy')}
          </button>
        </div>
      </form>
    </div>
  )
}

// Действия и эффекты повторяются по магическим навыкам — конфигурации навык не важен.
const uniqueByCode = (rows: Spell[]) =>
  rows.filter((row, i) => rows.findIndex(x => x.nameEn === row.nameEn) === i)

interface SpellProps {
  title: string
  system: GameSystem
  onConfirm: (value: string) => void
  onCancel: () => void
}

/**
 * Сборщик конфигурации Signature Spell: одно магическое действие и непустой набор его
 * дополнительных эффектов. Повторяемые эффекты (Дистанция, Размер) можно добавить несколько раз.
 */
export function SignatureSpellDialog({ title, system, onConfirm, onCancel }: SpellProps) {
  const [spells, setSpells] = useState<Spell[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [action, setAction] = useState('')
  const [effects, setEffects] = useState<string[]>([])

  useEffect(() => {
    let active = true
    api.spells(system)
      .then(rows => { if (active) setSpells(rows) })
      .catch((err: unknown) => {
        if (active) setLoadError(err instanceof Error ? err.message : t('Ошибка загрузки магии', 'Failed to load magic'))
      })
    return () => { active = false }
  }, [system])

  const name = useMemo(() => spellNameResolver(spells ?? []), [spells])
  const actions = useMemo(
    () => uniqueByCode((spells ?? []).filter(s => s.kind === 'effect'))
      .toSorted((a, b) => name(a.nameEn).localeCompare(name(b.nameEn), lang)),
    [spells, name])
  const activeAction = action || actions[0]?.nameEn || ''
  const available = useMemo(
    () => uniqueByCode((spells ?? []).filter(s => s.kind === 'additionalEffect' && s.parentEffect === activeAction)),
    [spells, activeAction])

  const countOf = (code: string) => effects.filter(x => x === code).length
  // Взаимоисключение проверяется в обе стороны, как на сервере: достаточно, чтобы его назвал
  // любой из двух эффектов.
  const conflicts = (effect: Spell) => [...new Set(effects)].filter(code => code !== effect.nameEn
    && (effect.exclusions.includes(code)
      || available.some(other => other.nameEn === code && other.exclusions.includes(effect.nameEn))))
  const add = (effect: Spell) => setEffects(prev => [...prev, effect.nameEn])
  const removeOne = (code: string) => setEffects(prev => {
    const i = prev.indexOf(code)
    return i < 0 ? prev : prev.toSpliced(i, 1)
  })

  const chooseAction = (code: string) => {
    setAction(code)
    setEffects([])
  }

  const configuration = { action: activeAction, effects }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()}
        onSubmit={e => {
          e.preventDefault()
          if (activeAction && effects.length > 0) onConfirm(signatureSpellValue(configuration))
        }}>
        <h3>{title}</h3>
        <p className="hint">
          {t('Закрепите одно магическое действие и точный набор его дополнительных эффектов — сложность именно этой комбинации снижается на 1.',
            'Fix one magic action and the exact set of its additional effects — the difficulty of that combination is reduced by 1.')}
        </p>
        {loadError ? (
          <p className="error">{loadError}</p>
        ) : spells === null ? (
          <p className="muted">{t('Загружаем справочник магии…', 'Loading magic reference…')}</p>
        ) : actions.length === 0 ? (
          <p className="muted">{t('В справочнике нет магических действий.', 'The reference has no magic actions.')}</p>
        ) : (
          <>
            <label>{t('Магическое действие', 'Magic action')}
              <select value={activeAction} onChange={e => chooseAction(e.target.value)}>
                {actions.map(a => <option key={a.nameEn} value={a.nameEn}>{name(a.nameEn)}</option>)}
              </select>
            </label>
            <div className="talent-group-title">{t('Дополнительные эффекты', 'Additional effects')}</div>
            <div className="chips">
              {available.map(effect => {
                const count = countOf(effect.nameEn)
                const blocked = conflicts(effect)
                const on = count > 0
                return (
                  <span key={effect.nameEn} className="talent-choice-effect">
                    <button type="button" aria-pressed={on}
                      className={on ? 'chip active' : 'chip'}
                      disabled={!on && blocked.length > 0}
                      title={blocked.length > 0
                        ? t(`Не сочетается с: ${blocked.map(code => name(code, activeAction)).join(', ')}`,
                          `Cannot be combined with: ${blocked.map(code => name(code, activeAction)).join(', ')}`)
                        : effect.repeatable ? t('Можно добавить несколько раз', 'Can be added several times') : ''}
                      onClick={() => (on && !effect.repeatable ? removeOne(effect.nameEn) : add(effect))}>
                      {name(effect.nameEn, activeAction)} <span className="muted">{effect.difficulty}</span>
                      {count > 1 && <span> ×{count}</span>}
                    </button>
                    {on && effect.repeatable && (
                      <button type="button" className="small"
                        aria-label={t(`Убрать один «${name(effect.nameEn, activeAction)}»`, `Remove one "${name(effect.nameEn, activeAction)}"`)}
                        onClick={() => removeOne(effect.nameEn)}>−</button>
                    )}
                  </span>
                )
              })}
            </div>
            <p className="bonus-line">
              {effects.length > 0
                ? signatureSpellLabel(configuration, name)
                : t('Выберите хотя бы один дополнительный эффект.', 'Choose at least one additional effect.')}
            </p>
          </>
        )}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>{t('Отмена', 'Cancel')}</button>
          <button type="submit" className="primary" disabled={!activeAction || effects.length === 0}>
            {t('Подтвердить и купить', 'Confirm and buy')}
          </button>
        </div>
      </form>
    </div>
  )
}
