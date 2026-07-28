import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { DicePool, GameSystem, ItemImplement, Spell } from '../api/types'
import {
  difficultyLabel, localizedDescription, magicSkillLabel, MAX_SPELL_DIFFICULTY, parseDifficulty, wouldExceedSpellCap,
} from '../utils/labels'
import { implementDifficulty, implementDiscounts, implementWorks } from '../utils/implements'
import { IMPLEMENT_MATERIAL_LABELS } from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { PrintPreview } from './print/PrintPreview'
import { MagicActionCard, type MagicCardData } from './print/cards'
import { magicMarkdown } from './print/markdown'
import { t } from '../i18n'

export interface MagicSkillPool {
  name: string
  pool: DicePool
}

/** Магический инструмент персонажа, доступный сборщику: только то, что в руках. */
export interface BuilderImplement {
  /** Строка инвентаря — по ней ведущий настраивает фолиант и палочку. */
  itemId: string
  name: string
  implement: ItemImplement
}

interface Props {
  system: GameSystem
  /** Магические навыки персонажа с пулами кубов — для интеграции с листом (необязательно). */
  characterSkills?: MagicSkillPool[]
  /**
   * Инструменты в руках персонажа (ROT-MAG-IMP-01). На одну проверку работает ровно один,
   * поэтому сборщик даёт выбрать его, а не складывает все сразу.
   */
  implements?: BuilderImplement[]
  /** Настройка фолианта и палочки ведущим; без неё выбор эффектов не предлагается. */
  onConfigureImplement?: (itemId: string, effectCodes: string[]) => Promise<void>
  onError: (message: string) => void
}

/**
 * Magic Action Builder: пользователь выбирает направление магии и базовый эффект, отмечает
 * дополнительные эффекты, а сборщик считает итоговую сложность и собирает текст для копирования.
 * Работает поверх того же справочника, что и SpellsTab; персонажа знать не обязательно (режим GM).
 */
export function MagicBuilder({
  system, characterSkills, implements: tools, onConfigureImplement, onError,
}: Props) {
  // Инструмент выбирается явно: на одну магическую проверку работает ровно один, и складывать
  // посох с палочкой правила не дают.
  const [implementItemId, setImplementItemId] = useState('')
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

  // Инструмент удешевляет добавленные эффекты (ROT-MAG-IMP-01). Правило повторяет серверное:
  // сложность собирается из действия и эффектов, потом инструмент снимает свои надбавки, и итог
  // не опускается ниже базовой сложности действия.
  const activeTool = tools?.find(x => x.itemId === implementItemId) ?? null
  const discounts = implementDiscounts(activeTool?.implement ?? null, chosen, activeSkill)
  const discounted = discounts.reduce((sum, d) => sum + d.reduction, 0)
  const rawDifficulty = baseDifficulty + added
  // Добавление эффектов сверх потолка блокируется, поэтому сумма не превышает 5; min — страховка отображения.
  const totalDifficulty = Math.min(
    MAX_SPELL_DIFFICULTY, implementDifficulty(baseDifficulty, rawDifficulty, discounts))
  const capReached = baseDifficulty + added >= MAX_SPELL_DIFFICULTY
  const toolBoost = activeTool && implementWorks(activeTool.implement, activeSkill)
    && !activeTool.implement.pending
    ? activeTool.implement.boostDice
    : 0

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
      for (const a of chosen) lines.push(t(`  • ${a.nameRu} (${a.nameEn}) ${a.difficulty} — ${localizedDescription(a)}`, `  • ${a.nameEn} (${a.nameRu}) ${a.difficulty} — ${localizedDescription(a)}`))
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
    effects: chosen.map(a => ({ ru: a.nameRu, en: a.nameEn, difficulty: a.difficulty, summary: localizedDescription(a) })),
    description: selectedEffect ? localizedDescription(selectedEffect) : '',
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
            {t(`Ваш пул для «${magicSkillLabel(activeSkill)}»:`, `Your pool for “${magicSkillLabel(activeSkill)}”:`)} <DicePoolView pool={charPool} boost={toolBoost} />
          </div>
        )}
        {/* Инструмент в руках: ровно один на проверку (ROT-MAG-IMP-01). */}
        {tools && tools.length > 0 && (
          <div className="magic-implement small-text">
            <label className="inline-label">{t('Инструмент', 'Implement')}
              <select value={implementItemId} onChange={e => setImplementItemId(e.target.value)}>
                <option value="">{t('— без инструмента —', '— none —')}</option>
                {tools.map(x => (
                  <option key={x.itemId} value={x.itemId}>
                    {x.name} · {IMPLEMENT_MATERIAL_LABELS[x.implement.material]}
                  </option>
                ))}
              </select>
            </label>
            {activeTool && !implementWorks(activeTool.implement, activeSkill) && (
              <span className="muted">
                {t(`Работает только с направлением ${magicSkillLabel(activeTool.implement.requiredMagicSkill)}.`,
                  `Works only with ${magicSkillLabel(activeTool.implement.requiredMagicSkill)}.`)}
              </span>
            )}
            {activeTool?.implement.pending && (
              <ImplementConfigurator tool={activeTool} effects={additional}
                onConfigure={onConfigureImplement} onError={onError} />
            )}
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
          {added > 0 && (
            <div className="muted small-text">
              {t('Базовая', 'Base')} {baseDifficulty} {t('+ дополнительные', '+ additional')} {added}
              {discounted > 0 && t(` − инструмент ${discounted}`, ` − implement ${discounted}`)}
            </div>
          )}
          {/* Что именно удешевил инструмент — иначе непонятно, почему сложность ниже суммы. */}
          {discounts.length > 0 && (
            <div className="muted small-text">
              {t('Инструмент', 'Implement')}: {discounts.map(d => `${d.code} −${d.reduction}`).join(', ')}
            </div>
          )}
          {capReached && (
            <div className="muted small-text cap-note">
              {t(`Достигнут потолок сложности ${MAX_SPELL_DIFFICULTY} — новые эффекты добавить нельзя.`, `The difficulty cap of ${MAX_SPELL_DIFFICULTY} is reached — no more effects can be added.`)}
            </div>
          )}
          {chosen.length > 0 && (
            <div className="chips effect-summary">
              {chosen.map(a => (
                <span key={a.id} className="chip active removable" title={localizedDescription(a)}>
                  {t(a.nameRu, a.nameEn)} <span className="effect-chip-diff">{a.difficulty}</span>
                  <button type="button" aria-label={t(`Убрать эффект «${a.nameRu}»`, `Remove effect “${a.nameEn}”`)} onClick={() => toggle(a)}>×</button>
                </span>
              ))}
            </div>
          )}
          <p>{localizedDescription(selectedEffect)}</p>
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
                  const description = localizedDescription(a)
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
                      <div className="small-text">{localizedDescription(a)}</div>
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

/**
 * Настройка фолианта или палочки ведущим (ROT-MAG-IMP-01). Выбор делается один раз, когда
 * экземпляр изготовлен или впервые попал в руки, и дальше не меняется — поэтому кнопка одна,
 * а список эффектов берётся из того же справочника, что и сам сборщик.
 *
 * Границы книги проверяет сервер: палочка берёт ровно один эффект с надбавкой +1, у фолианта
 * рекомендованный бюджет надбавок — три. Здесь они только подсказаны.
 */
function ImplementConfigurator({ tool, effects, onConfigure, onError }: {
  tool: BuilderImplement
  effects: Spell[]
  onConfigure?: (itemId: string, effectCodes: string[]) => Promise<void>
  onError: (message: string) => void
}) {
  const [codes, setCodes] = useState<string[]>([])
  const [busy, setBusy] = useState(false)
  const spec = tool.implement

  if (!onConfigure) {
    return (
      <span className="damage-warn">
        {t('Инструмент не настроен — бесплатный эффект выбирает ведущий.',
          'The implement is not configured — the GM picks the free effect.')}
      </span>
    )
  }

  // Палочка берёт только эффекты с надбавкой ровно +1; фолиант — любые.
  const allowed = effects.filter(e => spec.choiceExactIncrease == null
    || parseDifficulty(e.difficulty) === spec.choiceExactIncrease)
  const toggle = (code: string) => setCodes(prev => prev.includes(code)
    ? prev.filter(x => x !== code)
    : prev.length >= spec.choiceCount ? prev : [...prev, code])

  return (
    <div className="implement-config">
      <span className="damage-warn">
        {t(`Не настроен: выберите ${spec.choiceCount === 1 ? 'эффект' : `до ${spec.choiceCount} эффектов`}.`,
          `Not configured: choose ${spec.choiceCount === 1 ? 'an effect' : `up to ${spec.choiceCount} effects`}.`)}
        {spec.choiceMaxIncreaseSum != null
          && t(` Сумма надбавок обычно не выше ${spec.choiceMaxIncreaseSum}.`,
            ` The increases usually total no more than ${spec.choiceMaxIncreaseSum}.`)}
      </span>
      <div className="chips">
        {allowed.map(e => (
          <button key={e.id} type="button"
            className={codes.includes(e.nameEn) ? 'chip active' : 'chip'}
            onClick={() => toggle(e.nameEn)}>
            {t(e.nameRu, e.nameEn)} <span className="effect-chip-diff">{e.difficulty}</span>
          </button>
        ))}
        {allowed.length === 0 && (
          <span className="muted">
            {t('У этого базового эффекта нет подходящих дополнительных эффектов.',
              'This base effect has no suitable additional effects.')}
          </span>
        )}
      </div>
      <button type="button" className="primary small" disabled={busy || codes.length === 0}
        onClick={() => {
          setBusy(true)
          onConfigure(tool.itemId, codes)
            .catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка', 'Error')))
            .finally(() => setBusy(false))
        }}>
        {t('Настроить', 'Configure')}
      </button>
    </div>
  )
}
