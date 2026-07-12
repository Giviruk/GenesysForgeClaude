import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { DicePool, GameSystem, Spell } from '../api/types'
import {
  difficultyLabel, magicSkillLabel, MAX_SPELL_DIFFICULTY, parseDifficulty, wouldExceedSpellCap,
} from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { PrintPreview } from './print/PrintPreview'
import { MagicActionCard, type MagicCardData } from './print/cards'
import { magicMarkdown } from './print/markdown'
import { t } from '../i18n'

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
  const [printing, setPrinting] = useState(false)

  const reload = useCallback(
    () => api.spells(system)
      .then(setSpells)
      .catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка загрузки магии', 'Failed to load magic'))),
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
  const chosenDifficulties = chosen.map(a => a.difficulty)
  const baseDifficulty = selectedEffect ? parseDifficulty(selectedEffect.difficulty) : 0
  const added = chosen.reduce((sum, a) => sum + parseDifficulty(a.difficulty), 0)
  // Добавление эффектов сверх потолка блокируется, поэтому сумма не превышает 5; min — страховка отображения.
  const totalDifficulty = Math.min(MAX_SPELL_DIFFICULTY, baseDifficulty + added)
  const capReached = baseDifficulty + added >= MAX_SPELL_DIFFICULTY

  // Пул кубов персонажа для выбранного направления (если передан лист).
  const charPool = characterSkills?.find(s => s.name === activeSkill)?.pool ?? null

  // Снять эффект можно всегда; добавить — только пока итоговая сложность не превысит потолок 5.
  const toggle = (effect: Spell) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(effect.id)) {
        next.delete(effect.id)
        return next
      }
      if (selectedEffect && wouldExceedSpellCap(selectedEffect.difficulty, chosenDifficulties, effect.difficulty))
        return prev
      next.add(effect.id)
      return next
    })
  }

  const buildText = (): string => {
    if (!selectedEffect) return ''
    const lines = [
      t(`Магическое действие — ${magicSkillLabel(activeSkill)}`, `Magic action — ${magicSkillLabel(activeSkill)}`),
      t(`Базовый эффект: ${selectedEffect.nameRu} (${selectedEffect.nameEn})`, `Base effect: ${selectedEffect.nameEn} (${selectedEffect.nameRu})`),
      t(`Сложность: ${totalDifficulty} (${difficultyLabel(totalDifficulty)})`, `Difficulty: ${totalDifficulty} (${difficultyLabel(totalDifficulty)})`)
        + (added > 0 ? t(` — базовая ${baseDifficulty} + ${added}`, ` — base ${baseDifficulty} + ${added}`) : ''),
    ]
    if (chosen.length) {
      lines.push(t('Доп. эффекты:', 'Additional effects:'))
      for (const a of chosen) lines.push(t(`  • ${a.nameRu} (${a.nameEn}) ${a.difficulty} — ${a.safeDescription || a.description}`, `  • ${a.nameEn} (${a.nameRu}) ${a.difficulty} — ${a.safeDescription || a.description}`))
    }
    const sources = [...new Set([selectedEffect.source, ...chosen.map(a => a.source)].filter(Boolean))]
    if (sources.length) lines.push(t(`Источники: ${sources.join('; ')}`, `Sources: ${sources.join('; ')}`))
    return lines.join('\n')
  }

  const cardData: MagicCardData = {
    skill: activeSkill,
    baseEffectRu: selectedEffect?.nameRu ?? '',
    baseEffectEn: selectedEffect?.nameEn ?? '',
    baseDifficulty,
    totalDifficulty,
    effects: chosen.map(a => ({ ru: a.nameRu, en: a.nameEn, difficulty: a.difficulty, summary: a.safeDescription || a.description })),
    description: selectedEffect ? (selectedEffect.description || selectedEffect.safeDescription) : '',
    sources: selectedEffect ? [...new Set([selectedEffect.source, ...chosen.map(a => a.source)].filter(Boolean))] : [],
    pool: charPool,
  }

  if (spells === null) return <p className="muted">{t('Загрузка…', 'Loading…')}</p>
  if (skills.length === 0) return <p className="muted">{t('Для этой системы нет магических направлений.', 'This system has no magic schools.')}</p>

  if (printing && selectedEffect) {
    return (
      <PrintPreview title={t(`Магическое действие — ${selectedEffect.nameRu}`, `Magic action — ${selectedEffect.nameEn}`)}
        markdown={() => magicMarkdown(cardData)} onClose={() => setPrinting(false)}>
        {() => <MagicActionCard data={cardData} />}
      </PrintPreview>
    )
  }

  return (
    <div className="magic-builder">
      <section className="panel">
        <div className="spells-head">
          <h3>{t('Сборка магического действия', 'Build a magic action')}</h3>
          <div className="spells-selectors">
            <label className="inline-label">{t('Направление', 'School')}
              <select value={activeSkill} onChange={e => setSkill(e.target.value)}>
                {skills.map(s => <option key={s} value={s}>{magicSkillLabel(s)}</option>)}
              </select>
            </label>
            <label className="inline-label">{t('Базовый эффект', 'Base effect')}
              <select value={activeEffectCode} onChange={e => setEffectCode(e.target.value)}>
                {baseEffects.map(e => <option key={e.id} value={e.nameEn}>{t(e.nameRu, e.nameEn)}</option>)}
              </select>
            </label>
          </div>
        </div>
        {charPool && (
          <div className="muted small-text">
            {t(`Ваш пул для «${magicSkillLabel(activeSkill)}»:`, `Your pool for “${magicSkillLabel(activeSkill)}”:`)} <DicePoolView pool={charPool} />
          </div>
        )}
      </section>

      {selectedEffect && (
        <section className="panel magic-result">
          <div className="spell-detail-head">
            <h3>{t(selectedEffect.nameRu, selectedEffect.nameEn)} <span className="muted">· {t(selectedEffect.nameEn, selectedEffect.nameRu)}</span></h3>
            <span className="difficulty-badge big">
              {t('Сложность:', 'Difficulty:')} {totalDifficulty} · {difficultyLabel(totalDifficulty)}
            </span>
          </div>
          <div className="difficulty-dice" aria-label={t(`${totalDifficulty} кубов сложности`, `${totalDifficulty} difficulty dice`)}>
            {Array.from({ length: totalDifficulty }).map((_, i) => <span key={i} className="die difficulty">▲</span>)}
            {totalDifficulty === 0 && <span className="muted">{t('— (простая проверка)', '— (simple check)')}</span>}
          </div>
          {added > 0 && <div className="muted small-text">{t('Базовая', 'Base')} {baseDifficulty} {t('+ дополнительные', '+ additional')} {added}</div>}
          {capReached && (
            <div className="muted small-text cap-note">
              {t(`Достигнут потолок сложности ${MAX_SPELL_DIFFICULTY} — новые эффекты добавить нельзя.`, `The difficulty cap of ${MAX_SPELL_DIFFICULTY} is reached — no more effects can be added.`)}
            </div>
          )}
          {chosen.length > 0 && (
            <div className="chips effect-summary">
              {chosen.map(a => (
                <span key={a.id} className="chip active removable" title={a.safeDescription || a.description}>
                  {t(a.nameRu, a.nameEn)} <span className="effect-chip-diff">{a.difficulty}</span>
                  <button type="button" aria-label={t(`Убрать эффект «${a.nameRu}»`, `Remove effect “${a.nameEn}”`)} onClick={() => toggle(a)}>×</button>
                </span>
              ))}
            </div>
          )}
          <p>{selectedEffect.description || selectedEffect.safeDescription}</p>
          <div className="muted small-text">{t('Источник:', 'Source:')} {selectedEffect.source}</div>
          <div className="card-actions">
            <CopyButton key={buildText()} text={buildText()} onError={onError} />
            <button className="small" onClick={() => setPrinting(true)}>{t('🖨 Печать карточки', '🖨 Print card')}</button>
          </div>
        </section>
      )}

      <section className="panel">
        <div className="spells-head">
          <h3>{t('Дополнительные эффекты', 'Additional effects')} {additional.length ? t(`(выбрано ${chosen.length} из ${additional.length})`, `(${chosen.length} of ${additional.length} selected)`) : ''}</h3>
          {capReached && additional.length > 0 && (
            <span className="difficulty-badge cap">{t(`потолок ${MAX_SPELL_DIFFICULTY} достигнут`, `cap of ${MAX_SPELL_DIFFICULTY} reached`)}</span>
          )}
        </div>
        <p className="hint">
          {t(
            `Каждый эффект повышает сложность на «+N». Итоговая сложность не может превышать ${MAX_SPELL_DIFFICULTY} — недоступные эффекты подсвечены и не добавляются.`,
            `Each effect raises the difficulty by “+N”. The total difficulty cannot exceed ${MAX_SPELL_DIFFICULTY} — unavailable effects are highlighted and cannot be added.`,
          )}
        </p>
        {additional.length === 0
          ? <p className="muted">{t('У этого базового эффекта нет дополнительных эффектов.', 'This base effect has no additional effects.')}</p>
          : (
            <>
              <div className="chips effect-chips">
                {additional.map(a => {
                  const on = selectedIds.has(a.id)
                  const blocked = !on && selectedEffect != null
                    && wouldExceedSpellCap(selectedEffect.difficulty, chosenDifficulties, a.difficulty)
                  const description = a.safeDescription || a.description
                  const title = blocked
                    ? t(`Недоступно: базовая ${baseDifficulty} + выбранные ${added} + ${a.difficulty} превысит потолок ${MAX_SPELL_DIFFICULTY}`,
                        `Unavailable: base ${baseDifficulty} + selected ${added} + ${a.difficulty} would exceed the cap of ${MAX_SPELL_DIFFICULTY}`)
                    : `${t(a.nameEn, a.nameRu)} · ${a.difficulty}${description ? ` — ${description}` : ''}`
                  return (
                    <button key={a.id} type="button"
                      className={`chip effect-chip${on ? ' active' : ''}${blocked ? ' blocked' : ''}`}
                      disabled={blocked}
                      aria-pressed={on}
                      title={title}
                      onClick={() => toggle(a)}>
                      {t(a.nameRu, a.nameEn)} <span className="effect-chip-diff">{a.difficulty}</span>
                    </button>
                  )
                })}
              </div>
              <details className="effect-descriptions">
                <summary>{t('Описания эффектов', 'Effect descriptions')} ({additional.length})</summary>
                <ul>
                  {additional.map(a => (
                    <li key={a.id}>
                      <strong>{t(a.nameRu, a.nameEn)}</strong> <span className="muted small-text">{t(a.nameEn, a.nameRu)}</span>{' '}
                      <span className="effect-chip-diff">{a.difficulty}</span>
                      <div className="small-text">{a.safeDescription || a.description}</div>
                    </li>
                  ))}
                </ul>
              </details>
            </>
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
      onError(t('Не удалось скопировать в буфер обмена.', 'Could not copy to the clipboard.'))
    }
  }
  return <button className="primary small" onClick={copy}>{copied ? t('Скопировано ✓', 'Copied ✓') : t('Скопировать карточку', 'Copy card')}</button>
}
