import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  DicePool, GameSystem, ItemImplement, ItemRuneboundShard, KnowledgeRating, Quality, ShardSpellEffect,
  SheetTalent, Spell,
} from '../api/types'
import {
  difficultyLabel, localizedDescription, magicSkillLabel, MAX_SPELL_DIFFICULTY,
} from '../utils/labels'
import {
  effectiveSpellDifficulty, implementDiscounts, implementWorks,
} from '../utils/implements'
import { IMPLEMENT_MATERIAL_LABELS } from '../utils/labels'
import { ImplementMaterialMemo } from './ImplementMaterialMemo'
import { DicePoolView } from './DicePoolView'
import { PrintPreview } from './print/PrintPreview'
import { MagicActionCard, type MagicCardData } from './print/cards'
import { magicMarkdown } from './print/markdown'
import { t } from '../i18n'
import { useDiceRoller } from '../dice-roller-store'
import { magicAdvantageSpends } from '../utils/magicRoller'
import { PropertyText } from './PropertyText'
import { canonicalQualityName } from '../data/itemQualities'
import { talentSpellEffects, type TalentSpellEffect } from '../utils/talentSpellEffects'

export interface MagicSkillPool {
  name: string
  characteristic: string
  characteristicValue: number
  pool: DicePool
  ranks: number
  isCareer: boolean
  setbackDice: number
  boostDice: number
  difficultyDice?: number
  difficultyUpgrades?: number
  removeBoosts?: boolean
}

/** Магический инструмент персонажа, доступный сборщику: только то, что в руках. */
export interface BuilderImplement {
  /** Строка инвентаря — по ней ведущий настраивает фолиант и палочку. */
  itemId: string
  name: string
  implement: ItemImplement
}

/** Runebound shard в руках; для Runes это отдельная, взаимоисключающая с implements категория. */
export interface BuilderShard {
  itemId: string
  name: string
  shard: ItemRuneboundShard
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
  shards?: BuilderShard[]
  /**
   * Откуда берётся рейтинг эффектов заклинания (ROT-MAG-10). Источники присылает сервер: выбор
   * между Преданиями и Запретным открывает талант, и решать это на клиенте нельзя.
   */
  knowledgeRating?: KnowledgeRating | null
  /** Качества из справочника для inline-тултипов в тексте эффекта. */
  qualities?: Quality[]
  /** Купленные таланты, чьи структурные правила могут удешевлять эффекты заклинания. */
  talents?: SheetTalent[]
  /** Настройка фолианта и палочки ведущим; без неё выбор эффектов не предлагается. */
  onConfigureImplement?: (itemId: string, effectCodes: string[]) => Promise<void>
  onConfigureLesserRune?: (
    itemId: string, activationDescription: string, actionCode: string, effectCode: string
  ) => Promise<void>
  onError: (message: string) => void
}

/**
 * Magic Action Builder: пользователь выбирает направление магии и базовый эффект, отмечает
 * дополнительные эффекты, а сборщик считает итоговую сложность и собирает текст для копирования.
 * Работает поверх того же справочника, что и SpellsTab; персонажа знать не обязательно (режим GM).
 */
export function MagicBuilder({
  system, characterSkills, knowledgeRating, implements: tools, shards,
  onConfigureImplement, onConfigureLesserRune, onError, qualities, talents,
}: Props) {
  const { openRoller } = useDiceRoller()
  // Инструмент выбирается явно: на одну магическую проверку работает ровно один, и складывать
  // посох с палочкой правила не дают.
  const [implementItemId, setImplementItemId] = useState('')
  const [spells, setSpells] = useState<Spell[] | null>(null)
  const [skill, setSkill] = useState('')
  const [effectCode, setEffectCode] = useState('')
  // Сколько раз выбран каждый эффект. Дистанцию и Размер книга разрешает добавлять несколько раз,
  // и каждое добавление снова стоит своей надбавки — множеством это не выражается.
  const [counts, setCounts] = useState<Record<string, number>>({})
  // Навык, по которому считается рейтинг свойств. Выбор есть только у того, кому его дало
  // правило: без «Тёмного прозрения» сервер присылает один источник (ROT-MAG-10).
  const [ratingSkill, setRatingSkill] = useState('')
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

  const runesSkill = characterSkills?.find(s => s.name === 'Runes') ?? null
  const canUseShard = characterSkills == null
    || (runesSkill?.isCareer === true && runesSkill.ranks >= 1)
  const activeTool = activeSkill === 'Runes'
    ? null
    : tools?.find(x => x.itemId === implementItemId) ?? null
  const activeShard = activeSkill === 'Runes' && canUseShard
    ? shards?.find(x => x.itemId === implementItemId) ?? null
    : null
  const selectedImplementItemId = activeTool?.itemId ?? activeShard?.itemId ?? ''
  const shardRequired = activeSkill === 'Runes' && characterSkills != null
    && (activeShard == null || activeShard.shard.pending)

  const shardEffects = useMemo<ShardSpellEffect[]>(() => {
    if (!activeShard || activeShard.shard.pending) return []
    const configured = activeShard.shard.spec.needsConfiguration
      && activeShard.shard.effectAction && activeShard.shard.effectChoice
      ? [{
          action: activeShard.shard.effectAction,
          effectCode: activeShard.shard.effectChoice,
          mode: 'optionalFree' as const,
          freeUses: 1,
          overridesSkillRestriction: false,
        }]
      : []
    return [...activeShard.shard.spec.spellEffects, ...configured]
      .filter(e => !e.action || e.action === activeEffectCode)
  }, [activeShard, activeEffectCode])
  const talentEffects = useMemo<TalentSpellEffect[]>(
    () => talentSpellEffects(talents), [talents])
  const talentEffectsForAction = useMemo(
    () => talentEffects.filter(e => !e.action || e.action === activeEffectCode),
    [talentEffects, activeEffectCode])
  const shardEffect = (effect: Spell) =>
    shardEffects.find(e => e.effectCode === effect.nameEn) ?? null
  const spellEffectRules = [...shardEffects, ...talentEffectsForAction]
  const mandatoryCodes = new Set(spellEffectRules
    .filter(e => e.mode === 'mandatoryFree').map(e => e.effectCode))

  // Доступность эффекта направлению — правило книги, а не оформление: «Рок» умеет только Магия,
  // и жрецу его выбрать нельзя (ROT-MAG-01). Матрица приходит с сервера полем allowedSkills.
  const availableToSkill = (effect: Spell) =>
    effect.allowedSkills.length === 0 || effect.allowedSkills.includes(activeSkill)
    || shardEffect(effect)?.overridesSkillRestriction === true

  // Развёрнутый список: повторно выбранный эффект встречается столько раз, сколько выбран, —
  // так и сложность, и скидка инструмента считаются по одному правилу. Недоступные направлению
  // эффекты в счёт не идут, даже если остались в counts после смены направления.
  const effectiveCount = (effect: Spell) =>
    Math.max(counts[effect.id] ?? 0, mandatoryCodes.has(effect.nameEn) ? 1 : 0)
  const chosen = additional.filter(availableToSkill)
    .flatMap(a => Array.from({ length: effectiveCount(a) }, () => a))
  const chosenCodes = new Set(chosen.map(a => a.nameEn))
  const missingMandatory = [...mandatoryCodes].some(code =>
    !additional.some(effect => effect.nameEn === code))
  const mandatoryConflict = chosen.some(effect =>
    mandatoryCodes.has(effect.nameEn)
    && effect.exclusions.some(code => chosenCodes.has(code)))
  const buildInvalid = shardRequired || missingMandatory || mandatoryConflict
  /** Эффект не сочетается с уже выбранным: «Отчаяние» и «Дополнительная цель» вместе не берутся. */
  const conflicts = (effect: Spell) => effect.exclusions
    .filter(code => chosenCodes.has(code) && code !== effect.nameEn)
  // Числа берутся полем, а не разбором печатной строки: сервер уже посчитал их по той же
  // записи справочника (ROT-MAG-01).
  const baseDifficulty = selectedEffect?.difficultyIncrease ?? 0
  const added = chosen.reduce((sum, a) => sum + a.difficultyIncrease, 0)

  // Инструмент удешевляет добавленные эффекты (ROT-MAG-IMP-01). Правило повторяет серверное:
  // сложность собирается из действия и эффектов, потом инструмент снимает свои надбавки, и итог
  // не опускается ниже базовой сложности действия.
  const discounts = implementDiscounts(activeTool?.implement ?? null, chosen, activeSkill)
  const shardDiscount = chosen.reduce((sum, effect, index, all) => {
    const rule = shardEffect(effect)
    if (!rule) return sum
    const occurrence = all.slice(0, index + 1).filter(x => x.nameEn === effect.nameEn).length
    return sum + (occurrence <= rule.freeUses ? effect.difficultyIncrease : 0)
  }, 0)
  const talentDiscount = chosen.reduce((sum, effect, index, all) => {
    const rule = talentEffectsForAction.find(e => e.effectCode === effect.nameEn)
    if (!rule) return sum
    const occurrence = all.slice(0, index + 1).filter(x => x.nameEn === effect.nameEn).length
    return sum + (occurrence <= rule.freeUses ? effect.difficultyIncrease : 0)
  }, 0)
  const flatShardReduction = activeShard?.shard.spec.difficultyReductions
    .filter(x => !x.action || x.action === activeEffectCode)
    .reduce((sum, x) => sum + x.amount, 0) ?? 0
  const discounted = discounts.reduce((sum, d) => sum + d.reduction, 0) + shardDiscount + talentDiscount
  const calculateDifficulty = (effects: Spell[]) => {
    const raw = baseDifficulty + effects.reduce((sum, x) => sum + x.difficultyIncrease, 0)
    const shardFree = effects.reduce((sum, effect, index, all) => {
      const rule = shardEffect(effect)
      if (!rule) return sum
      const occurrence = all.slice(0, index + 1).filter(x => x.nameEn === effect.nameEn).length
      return sum + (occurrence <= rule.freeUses ? effect.difficultyIncrease : 0)
    }, 0)
    const talentFree = effects.reduce((sum, effect, index, all) => {
      const rule = talentEffectsForAction.find(e => e.effectCode === effect.nameEn)
      if (!rule) return sum
      const occurrence = all.slice(0, index + 1).filter(x => x.nameEn === effect.nameEn).length
      return sum + (occurrence <= rule.freeUses ? effect.difficultyIncrease : 0)
    }, 0)
    const withImplement = activeShard
      ? raw - shardFree - flatShardReduction
      : effectiveSpellDifficulty(baseDifficulty, effects, activeTool?.implement ?? null, activeSkill)
    return Math.max(baseDifficulty, withImplement - talentFree)
  }
  const activeToolWorks = activeTool != null && implementWorks(activeTool.implement, activeSkill)
  const toolDamageDifficulty = activeToolWorks
    ? activeTool.implement.damageDifficultyIncrease
    : 0
  // Потолок считается по итоговой сложности, а не по сырой сумме надбавок: эффект, который
  // инструмент делает бесплатным, в предел не упирается.
  const totalDifficulty = Math.min(
    MAX_SPELL_DIFFICULTY,
    calculateDifficulty(chosen) + toolDamageDifficulty)
  const capReached = totalDifficulty >= MAX_SPELL_DIFFICULTY
  /** Добавление этого эффекта упрётся в потолок — с учётом того, что инструмент может его удешевить. */
  const wouldExceedCap = (candidate: Spell) =>
    calculateDifficulty([...chosen, candidate]) + toolDamageDifficulty > MAX_SPELL_DIFFICULTY
  // Что инструмент удешевляет — известно заранее, до выбора эффектов: у посоха и скипетра это
  // фиксированные эффекты, у фолианта и палочки — выбор ведущего.
  const toolActive = activeToolWorks && !activeTool.implement.pending
  const toolFreeEffectCodes = activeShard
    ? shardEffects.map(x => x.effectCode)
    : !toolActive ? [] : (
    activeTool!.implement.discount === 'chosenEffects'
      ? activeTool!.implement.chosenEffects
      : activeTool!.implement.discountEffects)
  const talentFreeEffectCodes = talentEffectsForAction.map(x => x.effectCode)
  const freeEffectCodes = [...new Set([...toolFreeEffectCodes, ...talentFreeEffectCodes])]
  const toolBoost = activeToolWorks && !activeTool.implement.pending
    ? activeTool.implement.boostDice
    : 0
  const toolDamageSetback = activeToolWorks ? activeTool.implement.damageSetbackDice : 0

  // Источник рейтинга: выбранный игроком либо первый — тот, что называют правила системы.
  const ratingOptions = knowledgeRating?.options ?? []
  const activeRating = ratingOptions.find(o => o.skill === ratingSkill) ?? ratingOptions[0] ?? null
  /** Рейтинг, который получат свойства этого эффекта; null — рейтинг тут ни при чём. */
  const ratingFor = (effect: Spell) =>
    effect.usesKnowledgeRating && activeRating ? activeRating.ranks : null
  /**
   * Подпись рейтинга: «Жжение 2» у свойств, «по Знанию 2» у эффектов, где по рангам считается
   * само число (раны Ядовитого, защита Добавления защиты). Ради этого числа игрок и открывает
   * сборщик за столом — искать его в описании не нужно.
   */
  const ratingLabel = (effect: Spell) => {
    const rating = ratingFor(effect)
    if (rating == null) return ''
    return effect.ratedQualities.length > 0
      ? effect.ratedQualities.map(q => `${canonicalQualityName(q.nameEn || q.nameRu)} ${rating}`).join(', ')
      : t(`по Знанию ${rating}`, `Knowledge ${rating}`)
  }
  /** У этого действия есть эффекты, чьи числа зависят от Знания. */
  const ratingMatters = additional.some(a => a.usesKnowledgeRating)

  // Пул кубов персонажа для выбранного направления (если передан лист).
  const activeCharacterSkill = characterSkills?.find(s => s.name === activeSkill) ?? null
  const charPool = activeCharacterSkill?.pool ?? null
  const rollLabel = selectedEffect
    ? t(
        `${magicSkillLabel(activeSkill)} · ${selectedEffect.nameRu}`,
        `${magicSkillLabel(activeSkill)} · ${selectedEffect.nameEn}`,
      )
    : magicSkillLabel(activeSkill)
  const isMagicAttack = selectedEffect?.nameEn === 'Attack'
  const characteristicMultiplier = chosenCodes.has('Empowered') ? 2 : 1
  const implementAttackBonus = activeTool && implementWorks(activeTool.implement, activeSkill)
    ? activeTool.implement.attackDamageBonus
    : 0
  const shardAttackBonus = activeShard?.shard.spec.attackDamageBonus ?? 0
  const attackImplementBonus = implementAttackBonus + shardAttackBonus
  const magicDamage = isMagicAttack && activeCharacterSkill
    ? {
        base: activeCharacterSkill.characteristicValue * characteristicMultiplier + attackImplementBonus,
        characteristic: activeCharacterSkill.characteristicValue,
        characteristicMultiplier,
        implementBonus: attackImplementBonus,
        successMultiplier: 1,
        ...(chosenCodes.has('Holy/Unholy')
          ? {
              conditionalSuccessMultiplier: 2,
              conditionalLabelRu: 'Если цель — враг веры или божества',
              conditionalLabelEn: 'If the target is an enemy of the faith or deity',
            }
          : {}),
      }
    : null

  // Снять эффект можно всегда; добавить — только доступный направлению, сочетаемый с уже
  // выбранными и не выводящий сложность за потолок 5.
  const add = (effect: Spell) => {
    if (buildInvalid) return
    if (!availableToSkill(effect) || conflicts(effect).length > 0) return
    if (selectedEffect && wouldExceedCap(effect)) return
    setCounts(prev => ({ ...prev, [effect.id]: (prev[effect.id] ?? 0) + 1 }))
  }
  const remove = (effect: Spell) => {
    if (mandatoryCodes.has(effect.nameEn)) return
    setCounts(prev => {
      const left = (prev[effect.id] ?? 0) - 1
      const next = { ...prev }
      if (left > 0) next[effect.id] = left
      else delete next[effect.id]
      return next
    })
  }
  /** Нажатие по чипу: у повторяемого эффекта добавляет ещё одно, у обычного — включает и выключает. */
  const toggle = (effect: Spell) => {
    if (effectiveCount(effect) > 0 && !effect.repeatable) remove(effect)
    else add(effect)
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
      for (const a of additional.filter(x => effectiveCount(x) > 0)) {
        const times = effectiveCount(a) > 1 ? ` ×${effectiveCount(a)}` : ''
        const rating = ratingLabel(a) ? ` [${ratingLabel(a)}]` : ''
        lines.push(t(
          `  • ${a.nameRu} (${a.nameEn}) ${a.difficulty}${times}${rating} — ${localizedDescription(a)}`,
          `  • ${a.nameEn} (${a.nameRu}) ${a.difficulty}${times}${rating} — ${localizedDescription(a)}`))
      }
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
    effects: additional.filter(a => effectiveCount(a) > 0).map(a => ({
      ru: a.nameRu, en: a.nameEn,
      difficulty: effectiveCount(a) > 1 ? `${a.difficulty} ×${effectiveCount(a)}` : a.difficulty,
      // Рейтинг попадает и на печатную карточку: её берут за стол, где приложения нет.
      summary: ratingLabel(a)
        ? `${ratingLabel(a)} — ${localizedDescription(a)}`
        : localizedDescription(a),
    })),
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
                {/* Опциональная книга помечена прямо в списке: игрок должен видеть, что берёт
                    контент Expanded Player's Guide, а не базовые правила (ROT-MAG-01). */}
                {baseEffects.map(e => (
                  <option key={e.id} value={e.nameEn}>
                    {t(e.nameRu, e.nameEn)}{e.isOptional ? ' · EPG' : ''}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
        {charPool && (
          <div className="muted small-text">
            {t(`Ваш пул для «${magicSkillLabel(activeSkill)}»:`, `Your pool for “${magicSkillLabel(activeSkill)}”:`)} <DicePoolView
              pool={charPool}
              boost={activeCharacterSkill?.removeBoosts
                ? 0
                : (activeCharacterSkill?.boostDice ?? 0) + toolBoost}
              setback={(activeCharacterSkill?.setbackDice ?? 0) + toolDamageSetback}
              difficulty={activeCharacterSkill?.difficultyDice}
              challenge={activeCharacterSkill?.difficultyUpgrades} />
          </div>
        )}
        {/* Откуда берётся рейтинг свойств (ROT-MAG-10). Выбор показывается только тогда, когда он
            действительно есть: «Тёмное прозрение» разрешает считать по Запретному знанию. */}
        {activeRating && ratingMatters && (
          <div className="magic-rating small-text">
            {ratingOptions.length > 1
              ? (
                <>
                  <label className="inline-label">{t('Рейтинг по навыку', 'Rating from')}
                    <select value={activeRating.skill} onChange={e => setRatingSkill(e.target.value)}>
                      {ratingOptions.map(o => (
                        <option key={o.skill} value={o.skill}>
                          {t(o.skillRu, o.skill)} · {o.ranks}
                        </option>
                      ))}
                    </select>
                  </label>
                  <span className="muted">
                    {t('«Тёмное прозрение» разрешает считать рейтинг по Запретному знанию.',
                      'Dark Insight lets the rating come from Knowledge (Forbidden).')}
                  </span>
                </>
              )
              : (
                <span className="muted">
                  {t(`Рейтинг свойств: ${activeRating.skillRu} ${activeRating.ranks}`,
                    `Quality rating: ${activeRating.skill} ${activeRating.ranks}`)}
                  {activeRating.ranks === 0 && t(' — рангов нет, рейтинг 0.', ' — no ranks, rating 0.')}
                </span>
              )}
          </div>
        )}

        {/* Для Runes выбирается shard; обычные implements с этим навыком не работают. */}
        {activeSkill === 'Runes' && characterSkills && !canUseShard && (
          <div className="damage-warn small-text">
            {t('Runebound shard требует карьерный навык «Руны» минимум с 1 рангом.',
              'A runebound shard requires Runes as a career skill with at least 1 rank.')}
          </div>
        )}
        {activeSkill === 'Runes' && canUseShard && characterSkills && (!shards || shards.length === 0) && (
          <div className="damage-warn small-text">
            {t('Для Runes требуется ровно один исправный runebound shard в состоянии «Используется».',
              'Runes requires exactly one usable equipped runebound shard.')}
          </div>
        )}
        {activeSkill === 'Runes' && canUseShard && shards && shards.length > 0 && (
          <div className="magic-implement small-text">
            <label className="inline-label">{t('Runebound shard', 'Runebound shard')}
              <select value={selectedImplementItemId} onChange={e => setImplementItemId(e.target.value)}>
                <option value="">{t('— без руны —', '— none —')}</option>
                {shards.map(x => <option key={x.itemId} value={x.itemId}>{x.name}</option>)}
              </select>
            </label>
            {activeShard && (
              <span className="implement-summary muted">
                {!activeShard.shard.pending && activeShard.shard.spec.attackDamageBonus > 0
                  && t(`урон Атаки +${activeShard.shard.spec.attackDamageBonus}`,
                    `Attack damage +${activeShard.shard.spec.attackDamageBonus}`)}
                {activeShard.shard.spec.castingStrainReduction > 0
                  && t(` · напряжение за заклинание −${activeShard.shard.spec.castingStrainReduction}`,
                    ` · casting strain −${activeShard.shard.spec.castingStrainReduction}`)}
                {flatShardReduction > 0
                  && t(` · итоговая сложность −${flatShardReduction} (не ниже 0)`,
                    ` · final difficulty −${flatShardReduction} (minimum 0)`)}
                {shardEffects.length > 0 && (
                  <> · {t('эффекты руны', 'shard effects')}: {shardEffects
                    .map(x => `${x.effectCode}${x.mode === 'mandatoryFree'
                      ? t(' (обязательно)', ' (mandatory)')
                      : t(' (бесплатно)', ' (free)')}`).join(', ')}</>
                )}
              </span>
            )}
            {activeShard?.shard.spec.activationAttack && (
              <span className="muted">
                {t('Самостоятельная активация', 'Standalone activation')}: {' '}
                {activeShard.shard.spec.activationCost} · {activeShard.shard.spec.activationAttack.skill},
                {' '}{t('урон', 'damage')} {activeShard.shard.spec.activationAttack.damage},
                {' '}{t('крит', 'crit')} {activeShard.shard.spec.activationAttack.critical},
                {' '}{activeShard.shard.spec.activationAttack.range}.
                {' '}{t('Приложение показывает профиль, но активацию и длительность отмечает стол.',
                  'The app shows the profile; activation and duration are tracked at the table.')}
              </span>
            )}
            {activeShard?.shard.pending && (
              <LesserRuneConfigurator shard={activeShard} effects={additional}
                actionCode={activeEffectCode} onConfigure={onConfigureLesserRune} onError={onError} />
            )}
          </div>
        )}
        {activeSkill !== 'Runes' && tools && tools.length > 0 && (
          <div className="magic-implement small-text">
            <label className="inline-label">{t('Инструмент', 'Implement')}
              <select value={selectedImplementItemId} onChange={e => setImplementItemId(e.target.value)}>
                <option value="">{t('— без инструмента —', '— none —')}</option>
                {tools.map(x => (
                  <option key={x.itemId} value={x.itemId}>
                    {x.name} · {IMPLEMENT_MATERIAL_LABELS[x.implement.material]}
                  </option>
                ))}
              </select>
            </label>
            {/* Свойство материала срабатывает как раз в момент броска, поэтому памятка стоит
                рядом с выбранным инструментом (ROT-MAG-MAT-01). */}
            {activeTool && (
              <span className="muted">
                {IMPLEMENT_MATERIAL_LABELS[activeTool.implement.material]}
                {' '}<ImplementMaterialMemo material={activeTool.implement.material} />
              </span>
            )}
            {/* Что инструмент даёт — до выбора эффектов, а не после: иначе непонятно, зачем
                вообще брать фолиант (ROT-MAG-IMP-01). */}
            {toolActive && (
              <span className="implement-summary muted">
                {activeTool!.implement.attackDamageBonus > 0
                  && t(`урон Атаки +${activeTool!.implement.attackDamageBonus}`,
                    `Attack damage +${activeTool!.implement.attackDamageBonus}`)}
                {activeTool!.implement.boostDice > 0
                  && t(` · +${activeTool!.implement.boostDice} бонусная кость`,
                    ` · +${activeTool!.implement.boostDice} boost`)}
                {toolFreeEffectCodes.length > 0 && (
                  <> · {t('бесплатно', 'free')}: {toolFreeEffectCodes
                    .map(code => t(
                      additional.find(e => e.nameEn === code)?.nameRu || code,
                      code))
                    .join(', ')}
                    {activeTool!.implement.discount === 'firstNamedEffect'
                      && t(' (первое добавление)', ' (first use only)')}
                  </>
                )}
                {activeTool!.implement.discount === 'restrictedSkillDiscount'
                  && t(` · каждый эффект только для ${magicSkillLabel(activeTool!.implement.requiredMagicSkill)} дешевле на 1`,
                    ` · every ${magicSkillLabel(activeTool!.implement.requiredMagicSkill)}-only effect costs 1 less`)}
              </span>
            )}
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
        {talentFreeEffectCodes.length > 0 && (
          <div className="talent-spell-summary muted small-text">
            {t('Таланты дают бесплатные эффекты:', 'Talents grant free effects:')}{' '}
            {talentEffectsForAction.map(effect => {
              const spell = additional.find(x => x.nameEn === effect.effectCode)
              return `${t(spell?.nameRu || effect.effectCode, effect.effectCode)}${effect.mode === 'mandatoryFree'
                ? t(' (обязательно)', ' (mandatory)') : t(' (по выбору)', ' (optional)')}`
            }).join(', ')}
          </div>
        )}
      </section>

      {selectedEffect && (
        <section className="panel magic-result">
          {buildInvalid && (
            <div className="damage-warn">
              {shardRequired
                ? t('Сборка недействительна: выберите и настройте runebound shard. Проверка и стоимость не исполняются.',
                    'Invalid build: select and configure a runebound shard. No check or cost is resolved.')
                : t('Сборка недействительна: обязательный бесплатный эффект отсутствует или конфликтует с выбранным эффектом.',
                    'Invalid build: a mandatory free effect is missing or conflicts with a selected effect.')}
            </div>
          )}
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
              {discounted > 0 && t(` − скидки ${discounted}`, ` − discounts ${discounted}`)}
              {flatShardReduction > 0 && ` − ${flatShardReduction}`}
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
              {/* По одному чипу на эффект, даже если он добавлен несколько раз: крестик снимает
                  одно добавление, а не весь набор. */}
              {additional.filter(a => effectiveCount(a) > 0).map(a => (
                <span key={a.id} className={`chip active${mandatoryCodes.has(a.nameEn) ? '' : ' removable'}`}
                  title={localizedDescription(a)}>
                  {t(a.nameRu, a.nameEn)} <span className="effect-chip-diff">{a.difficulty}</span>
                  {effectiveCount(a) > 1 && (
                    <span className="effect-chip-count"> ×{effectiveCount(a)}</span>
                  )}
                  {mandatoryCodes.has(a.nameEn)
                    ? <span className="effect-chip-free"> {t('обязательно', 'mandatory')}</span>
                    : <button type="button"
                      aria-label={t(`Убрать эффект «${a.nameRu}»`, `Remove effect “${a.nameEn}”`)}
                      onClick={() => remove(a)}>×</button>}
                </span>
              ))}
            </div>
          )}
          <p><PropertyText text={localizedDescription(selectedEffect)} qualities={qualities} /></p>
          <div className="muted small-text">{t('Источник:', 'Source:')} {selectedEffect.source}</div>
          {!buildInvalid && (
            <div className="card-actions">
              {activeCharacterSkill && (
                <button type="button" className="primary small"
                  title={t('Бросить рассчитанный пул магического действия', 'Roll the calculated magic-action pool')}
                  onClick={() => openRoller({
                    kind: 'magic',
                    title: t('Магическая проверка', 'Magic check'),
                    label: rollLabel,
                    skillLabel: `${magicSkillLabel(activeSkill)} (${activeCharacterSkill.characteristicValue})`,
                    basePool: {
                      ability: activeCharacterSkill.pool.ability,
                      proficiency: activeCharacterSkill.pool.proficiency,
                      difficulty: totalDifficulty + (activeCharacterSkill.difficultyDice ?? 0),
                      ...((activeCharacterSkill.difficultyUpgrades ?? 0) > 0
                        ? { challenge: activeCharacterSkill.difficultyUpgrades }
                        : {}),
                      boost: activeCharacterSkill.removeBoosts
                        ? 0
                        : activeCharacterSkill.boostDice + toolBoost,
                      setback: activeCharacterSkill.setbackDice + toolDamageSetback,
                    },
                    damage: magicDamage,
                    advantageSpends: selectedEffect
                      ? magicAdvantageSpends(selectedEffect, chosen)
                      : [],
                  })}>
                  {t('🎲 Бросить', '🎲 Roll')}
                </button>
              )}
              <CopyButton key={buildText()} text={buildText()} onError={onError} />
              <button className="small" onClick={() => setPrinting(true)}>{t('🖨 Печать карточки', '🖨 Print card')}</button>
            </div>
          )}
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
                  const unavailable = !availableToSkill(a)
                  const conflicting = conflicts(a)
                  const count = unavailable ? 0 : effectiveCount(a)
                  const on = count > 0
                  // Повторяемый эффект добавляется снова, поэтому потолок его касается всегда,
                  // а не только при первом выборе.
                  const overCap = (!on || a.repeatable) && selectedEffect != null && wouldExceedCap(a)
                  const mandatory = mandatoryCodes.has(a.nameEn)
                  const blocked = buildInvalid || unavailable || conflicting.length > 0 || overCap || mandatory
                  // Источник бесплатного эффекта виден на самом чипе, а не только в итоговой
                  // сложности: это может быть инструмент, руна или талант.
                  const freeByTool = toolFreeEffectCodes.includes(a.nameEn)
                  const freeByTalent = talentFreeEffectCodes.includes(a.nameEn)
                  const freeBySource = freeEffectCodes.includes(a.nameEn)
                  const description = localizedDescription(a)
                  const freeNote = freeByTalent
                    ? t('\nТалант делает этот эффект бесплатным.',
                      '\nA talent makes this effect free.')
                    : freeByTool
                      ? t('\nИнструмент делает этот эффект бесплатным.',
                        '\nThe implement makes this effect free.')
                    : ''
                  // Причина недоступности называется прямо: иначе непонятно, эффект вообще не для
                  // этого направления, мешает уже выбранный или дело в потолке сложности.
                  const conflictNames = conflicting
                    .map(code => t(additional.find(x => x.nameEn === code)?.nameRu || code, code))
                    .join(', ')
                  const title = unavailable
                    ? t(`Недоступно направлению «${magicSkillLabel(activeSkill)}»: эффект только для ${a.allowedSkills.map(magicSkillLabel).join(', ')}`,
                        `Unavailable for ${magicSkillLabel(activeSkill)}: this effect is ${a.allowedSkills.map(magicSkillLabel).join(', ')} only`)
                    : conflicting.length > 0
                      ? t(`Не сочетается с выбранным: ${conflictNames}`,
                          `Cannot be combined with the selected ${conflictNames}`)
                      : overCap
                        ? t(`Недоступно: базовая ${baseDifficulty} + выбранные ${added} + ${a.difficulty} превысит потолок ${MAX_SPELL_DIFFICULTY}`,
                            `Unavailable: base ${baseDifficulty} + selected ${added} + ${a.difficulty} would exceed the cap of ${MAX_SPELL_DIFFICULTY}`)
                        : `${t(a.nameEn, a.nameRu)} · ${a.difficulty}${description ? ` — ${description}` : ''}${freeNote}`
                  return (
                    <button key={a.id} type="button"
                      className={`chip effect-chip${on ? ' active' : ''}${blocked ? ' blocked' : ''}${freeBySource ? ' free' : ''}`}
                      disabled={blocked}
                      aria-pressed={on}
                      title={a.repeatable && on
                        ? t(`${title}\nМожно добавлять несколько раз; убрать — крестиком.`,
                          `${title}\nCan be added several times; remove with the ×.`)
                        : title}
                      onClick={() => toggle(a)}>
                      {t(a.nameRu, a.nameEn)}{' '}
                      <span className="effect-chip-diff">
                        {/* Зачёркнутая надбавка вместо просто «+1»: инструмент её снимает. */}
                        {freeBySource ? <s>{a.difficulty}</s> : a.difficulty}
                      </span>
                      {/* Дистанцию и Размер книга разрешает добавлять несколько раз — счётчик
                          показывает, сколько уже добавлено. */}
                      {count > 1 && <span className="effect-chip-count"> ×{count}</span>}
                      {freeBySource && <span className="effect-chip-free"> {t('бесплатно', 'free')}</span>}
                      {mandatory && <span className="effect-chip-free"> {t('обязательно', 'mandatory')}</span>}
                      {/* Рейтинг по Знанию — числом на чипе, а не отсылкой «равен рангу Знания». */}
                      {ratingLabel(a) && (
                        <span className="effect-chip-rating"> · <PropertyText text={ratingLabel(a)} qualities={qualities} /></span>
                      )}
                      {/* Ограничение видно на самом чипе: подсказка по наведению есть не везде. */}
                      {unavailable && (
                        <span className="effect-chip-only">
                          {' '}{t(`только ${a.allowedSkills.map(magicSkillLabel).join('/')}`,
                            `${a.allowedSkills.map(magicSkillLabel).join('/')} only`)}
                        </span>
                      )}
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
                      {/* Кому эффект доступен и с чем не сочетается — рядом с описанием, а не внутри него. */}
                      {a.restrictedSkill && (
                        <span className="muted small-text">
                          {' '}· {t(`только ${magicSkillLabel(a.restrictedSkill)}`,
                            `${magicSkillLabel(a.restrictedSkill)} only`)}
                        </span>
                      )}
                      {ratingLabel(a) && (
                        <span className="muted small-text"> · <PropertyText text={ratingLabel(a)} qualities={qualities} /></span>
                      )}
                      {a.exclusions.length > 0 && (
                        <span className="muted small-text">
                          {' '}· {t('не сочетается с', 'not combinable with')}{' '}
                          {a.exclusions.map(code => t(
                            additional.find(x => x.nameEn === code)?.nameRu || code, code)).join(', ')}
                        </span>
                      )}
                      <div className="small-text"><PropertyText text={localizedDescription(a)} qualities={qualities} /></div>
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
    || e.difficultyIncrease === spec.choiceExactIncrease)
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

/** Одноразовая настройка Lesser Rune: собственная активация и один эффект Runes с надбавкой +1. */
function LesserRuneConfigurator({ shard, effects, actionCode, onConfigure, onError }: {
  shard: BuilderShard
  effects: Spell[]
  actionCode: string
  onConfigure?: (
    itemId: string, activationDescription: string, actionCode: string, effectCode: string
  ) => Promise<void>
  onError: (message: string) => void
}) {
  const [activation, setActivation] = useState('')
  const [effectCode, setEffectCode] = useState('')
  const [busy, setBusy] = useState(false)
  const allowed = effects.filter(e => e.difficultyIncrease === 1
    && (e.allowedSkills.length === 0 || e.allowedSkills.includes('Runes')))
  const selected = allowed.some(e => e.nameEn === effectCode)
    ? effectCode
    : (allowed[0]?.nameEn ?? '')

  if (!onConfigure) {
    return <span className="damage-warn">
      {t('Lesser Rune не настроена: ведущий должен записать активацию и эффект +1.',
        'The Lesser Rune is pending: the GM must record its activation and one +1 effect.')}
    </span>
  }

  return (
    <div className="implement-config">
      <span className="damage-warn">
        {t('Настройка необратима: опишите самостоятельную активацию и выберите ровно один эффект +1.',
          'Configuration is permanent: describe the standalone activation and choose exactly one +1 effect.')}
      </span>
      <textarea value={activation} minLength={3} maxLength={500}
        placeholder={t('Описание активации (3–500 символов)', 'Activation description (3–500 characters)')}
        onChange={e => setActivation(e.target.value)} />
      <select value={selected} onChange={e => setEffectCode(e.target.value)}>
        {allowed.map(e => (
          <option key={e.id} value={e.nameEn}>{t(e.nameRu, e.nameEn)} · {e.difficulty}</option>
        ))}
      </select>
      <button type="button" className="primary small"
        disabled={busy || activation.trim().length < 3 || !selected}
        onClick={() => {
          setBusy(true)
          onConfigure(shard.itemId, activation.trim(), actionCode, selected)
            .catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка', 'Error')))
            .finally(() => setBusy(false))
        }}>
        {t('Настроить Lesser Rune', 'Configure Lesser Rune')}
      </button>
    </div>
  )
}
