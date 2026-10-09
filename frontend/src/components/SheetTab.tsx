import { BookReference } from './BookReference'
import { useState, type CSSProperties } from 'react'
import { api } from '../api/client'
import type {
  BaseSheet, CareerSkillSource, CharacterSheet, CheckModifierSource, DefenseBreakdown, Derived, SheetSkill, SkillKind,
} from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, CHARACTERISTIC_SHORT_LABELS, HEROIC_UPGRADE_LABELS,
  localizedDescription, localizedName, secondaryName, SKILL_KIND_LABELS,
} from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { CriticalInjuriesSection } from './CriticalInjuriesSection'
import { useDiceRoller } from '../dice-roller-store'
import { Icon } from './Icon'
import { FilterChip } from './content/ContentUi'
import { VitalCard } from './content/VitalCard'
import { purchasedPool } from '../utils/contentLibrary'
import { MAX_SKILL_RANK_AT_CREATION } from '../utils/rules'
import { t } from '../i18n'

interface Props {
  sheet: CharacterSheet
  onError: (message: string) => void
  refresh: () => Promise<void>
  updateBaseOptimistically?: (patch: Partial<BaseSheet>, action: () => Promise<unknown>) => Promise<void>
  readOnly?: boolean
}

// Левая колонка — крупный блок «общие»; правая — боевые, под ними знания/магия и
// социальные, чтобы плотно заполнить пространство и меньше скроллить.
const SKILL_COLUMNS: SkillKind[][] = [
  ['general'],
  ['combat', 'social', 'magic', 'knowledge'],
]

const CAREER_SOURCE_LABELS: Record<CareerSkillSource['source'], string> = t({
  Career: 'карьера', Species: 'вид', Talent: 'талант',
}, {
  Career: 'career', Species: 'species', Talent: 'talent',
})

/** Название источника помех в текущей локали; у перегруза своего предмета нет. */
function modifierSourceName(s: CheckModifierSource): string {
  const localized = t(s.sourceNameRu || s.sourceName, s.sourceName || s.sourceNameRu)
  if (localized) return localized
  return s.sourceType === 'Encumbrance' ? t('перегруз', 'encumbrance') : s.sourceType
}

/**
 * Подсказка «откуда модификаторы»: броня, перегруз, снаряжение и критические травмы.
 * Условные вклады отмечены отдельно — их приложение в пул не подставляет.
 */
function setbackTitle(skill: SheetSkill): string | undefined {
  const sources = skill.setbackSources ?? []
  if (sources.length === 0) return undefined
  const line = (s: CheckModifierSource) => {
    const parts: string[] = []
    if (s.setback !== 0) {
      const sign = s.setback > 0 ? '+' : '−'
      parts.push(`${sign}${Math.abs(s.setback)} ${t('помех', 'setback')}`)
    }
    if ((s.difficulty ?? 0) !== 0) parts.push(`+${s.difficulty} ${t('сложности', 'difficulty')}`)
    if ((s.difficultyUpgrades ?? 0) !== 0) {
      parts.push(`+${s.difficultyUpgrades} ${t('усил. сложности', 'difficulty upgrades')}`)
    }
    const boost = s.boost ?? 0
    if (boost !== 0) {
      const sign = boost > 0 ? '+' : '−'
      parts.push(`${sign}${Math.abs(boost)} ${t('бонусных', 'boost')}`)
    }
    if (s.removeBoosts) parts.push(t('без бонусных костей', 'no boost dice'))
    const body = `${modifierSourceName(s)}: ${parts.join(', ')}`
    return s.condition ? `${body} (${t('только', 'only')} ${s.condition})` : body
  }
  const head = t(
    `Модификаторы проверки: ${skill.setbackDice} помех, +${skill.difficultyDice ?? 0} сложности, +${skill.difficultyUpgrades ?? 0} усил.`,
    `Check modifiers: ${skill.setbackDice} setback, +${skill.difficultyDice ?? 0} difficulty, +${skill.difficultyUpgrades ?? 0} upgrades`,
  )
  return [head, ...sources.map(line)].join('\n')
}

/** Подсказка «почему навык карьерный»: перечисляет все источники статуса (ROT-CRE-01). */
function careerSourcesTitle(sources: CareerSkillSource[] | undefined): string | undefined {
  if (!sources?.length) return undefined
  return t('Карьерный навык: ', 'Career skill: ')
    + sources.map(s => `${CAREER_SOURCE_LABELS[s.source] ?? s.source} ${s.sourceName}`).join(', ')
}

export function SheetTab({ sheet, onError, refresh, updateBaseOptimistically, readOnly = false }: Props) {
  const { openRoller } = useDiceRoller()
  const [vitalsBusy, setVitalsBusy] = useState(false)
  const optimisticUpdate = updateBaseOptimistically ?? (async (_patch, action) => {
    await action()
    await refresh()
  })

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  async function updateVital(patch: Partial<BaseSheet>, action: () => Promise<unknown>) {
    if (vitalsBusy) return
    setVitalsBusy(true)
    try {
      await optimisticUpdate(patch, action)
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    } finally {
      setVitalsBusy(false)
    }
  }

  const [progressState, setProgress] = useState(() => ({ id: sheet.id, value: readProgress(sheet.id, sheet.isCreationPhase) }))
  const [previewSkill, setPreviewSkill] = useState<string | null>(null)
  const progress = !readOnly && (progressState.id === sheet.id ? progressState.value : readProgress(sheet.id, sheet.isCreationPhase))
  const maxDice = Math.max(0, ...sheet.skills.map(s => Math.max(s.pool.ability + s.pool.proficiency,
    s.ranks < 5 ? Math.max(sheet.characteristics[s.characteristic], s.ranks + 1) : 0)
    + (s.boostDice ?? 0) + (s.setbackDice ?? 0) + (s.difficultyDice ?? 0) + ((s.difficultyUpgrades ?? 0) > 0 ? 1 : 0)))
  const skillStyle = { '--skill-pool-width': `${Math.max(4.6, maxDice * .82 + Math.max(0, maxDice - 1) * .18)}rem` } as CSSProperties
  const d = sheet.derived

  return (
    <div>
      <section className="stat-row">
        {CHARACTERISTICS.map(c => (
          <div key={c} className="stat-box characteristic">
            <div className="stat-value">{sheet.characteristics[c]}</div>
            <div className="stat-label">{CHARACTERISTIC_LABELS[c]}</div>
            {!readOnly && sheet.isCreationPhase && (
              <div className="buy-row">
                {sheet.characteristics[c] > sheet.archetype[c] && (
                  <button className="small" title={t(`Вернуть ${sheet.characteristics[c] * 10} XP`, `Refund ${sheet.characteristics[c] * 10} XP`)}
                    onClick={() => run(() => api.refundCharacteristic(sheet.id, c))}>
                    −
                  </button>
                )}
                <button className="small" title={t(`Повысить за ${(sheet.characteristics[c] + 1) * 10} XP`, `Increase for ${(sheet.characteristics[c] + 1) * 10} XP`)}
                  onClick={() => run(() => api.buyCharacteristic(sheet.id, c))}>
                  +{(sheet.characteristics[c] + 1) * 10} XP
                </button>
              </div>
            )}
          </div>
        ))}
      </section>

      <section className="rd-derived-grid">
        <VitalCard kind="wounds" current={sheet.woundsCurrent} threshold={d.woundThreshold} disabled={vitalsBusy}
          onChange={readOnly ? undefined : value => void updateVital({ woundsCurrent: value }, () => api.updateCharacter(sheet.id, { woundsCurrent: value }))} />
        <VitalCard kind="strain" current={sheet.strainCurrent} threshold={d.strainThreshold} disabled={vitalsBusy}
          onChange={readOnly ? undefined : value => void updateVital({ strainCurrent: value }, () => api.updateCharacter(sheet.id, { strainCurrent: value }))} />
        <div className="rd-defense-card">
          <div title={statTitle(d.soakBreakdown, d.soak)}><Icon name="shield" /><b>{d.soak}</b><small>{t('Поглощение', 'Soak')}</small></div>
          <div title={defenseTitle(d)}><Icon name="shield" /><b>{d.meleeDefense} / {d.rangedDefense}</b><small>{t('Защита ближ. / дальн.', 'Defense melee / ranged')}</small></div>
          <div className={d.encumbered ? 'rd-warning' : ''} title={[statTitle(d.encumbranceThresholdBreakdown, d.encumbranceThreshold), encumbranceWarning(d)].filter(Boolean).join('\n')}>
            <Icon name="package" /><b>{d.encumbranceLoad} / {d.encumbranceThreshold}</b><small>{t('Переносимый вес', 'Encumbrance')}</small>
            <span className="rd-bar"><i style={{ width: `${Math.min(100, d.encumbranceThreshold > 0 ? d.encumbranceLoad / d.encumbranceThreshold * 100 : 100)}%` }} /></span>
          </div>
        </div>
        {d.encumbered && <p className="rd-encumbrance-warning">{encumbranceWarning(d)}</p>}
      </section>

      <CriticalInjuriesSection sheet={sheet} onError={onError} refresh={refresh} readOnly={readOnly} />

      {sheet.system === 'realmsOfTerrinoth' && <HeroicSummary sheet={sheet} />}

      <section className={`rd-skills${progress ? ' progress' : ''}`} style={skillStyle}>
        <header className="rd-skills-title"><h3>{t('Навыки', 'Skills')}</h3><span className="rd-heading-line" />
          <div className="rd-skill-legend"><span><i className="rd-career-mark active" />{t('карьерный', 'career')}</span><span className="ability">◆ {t('способность', 'ability')}</span><span className="proficiency">⬣ {t('мастерство', 'proficiency')}</span></div>
          {!readOnly && <FilterChip active={progress} onClick={() => {
            const next = !progress; setProgress({ id: sheet.id, value: next }); setPreviewSkill(null)
            try { localStorage.setItem(`genesysforge.skillProgress.${sheet.id}`, String(next)) } catch { /* Session-only when storage is unavailable. */ }
          }}>{t('Прокачка', 'Progression')}</FilterChip>}
        </header>
        {progress && <p className="hint">{t('Карьерный ранг стоит новый ранг × 5 XP, некарьерный — на 5 XP дороже. Наведите на цену для предпросмотра.', 'A career rank costs new rank × 5 XP; a non-career rank costs 5 XP more. Hover over the price to preview.')}</p>}
        <div className="rd-skills-grid">{SKILL_COLUMNS.map((kinds, i) => <div key={i} className="rd-skill-column">{kinds.map(kind => {
          const skills = sheet.skills.filter(s => s.kind === kind && (!s.unavailableReason || s.ranks > 0))
          if (!skills.length) return null
          return <section key={kind} className="rd-skill-group"><header className="rd-skill-grid-row"><strong>{SKILL_KIND_LABELS[kind]}</strong><span>{t('Ранг', 'Rank')}</span><span>{t('Пул', 'Pool')}</span><span>{progress ? 'XP' : ''}</span></header>
            {skills.map(s => {
              const label = localizedName(s), original = secondaryName(s)
              const unavailableReason = s.unavailableReason ? t(s.unavailableReason, 'Content is disabled in the campaign; new purchases are unavailable.') : undefined
              const canRefund = progress && sheet.isCreationPhase && s.ranks > s.freeRanks
              const maxRank = sheet.isCreationPhase ? MAX_SKILL_RANK_AT_CREATION : 5
              const canBuy = progress && s.ranks < maxRank && !s.unavailableReason && s.nextRankCost <= sheet.availableXp
              const preview = canBuy && previewSkill === s.skillDefId
              const nextPool = preview ? purchasedPool(sheet.characteristics[s.characteristic], s.ranks + 1) : s.pool
              return <div className={`rd-skill-grid-row${s.unavailableReason ? ' unavailable' : ''}${preview ? ' rd-skill-preview' : ''}`} key={s.skillDefId}>
                <div className="rd-skill-name"><i className={`rd-career-mark${s.isCareer ? ' active' : ''}`} title={careerSourcesTitle(s.careerSources)} />
                  <span title={[original ? `${label} / ${original}` : label, unavailableReason].filter(Boolean).join('\n')}>{label}</span>
                  <small title={CHARACTERISTIC_LABELS[s.characteristic]}>{CHARACTERISTIC_SHORT_LABELS[s.characteristic]}</small>
                </div><div className="rd-rank-diamonds" aria-label={t(`Ранги: ${s.ranks}`, `Ranks: ${s.ranks}`)}>{Array.from({ length: 5 }, (_, rank) => <i key={rank}
                  className={rank < s.ranks ? 'filled' : preview && rank === s.ranks ? 'preview' : ''} />)}</div>
                <div className="rd-skill-pool"><DicePoolView pool={nextPool} previousPool={preview ? s.pool : undefined} setback={s.setbackDice} boost={s.boostDice}
                  difficulty={s.difficultyDice} difficultyUpgrades={s.difficultyUpgrades} setbackTitle={setbackTitle(s)} /></div>
                <div className="rd-skill-actions">{progress && <button className={`rd-refund${canRefund ? '' : ' reserved'}`} tabIndex={canRefund ? 0 : -1} disabled={!canRefund}
                  aria-label={t(`Вернуть ранг: ${label}`, `Refund rank: ${label}`)} title={t(`Вернуть ранг ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`, `Refund rank ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`)}
                  onClick={() => void run(() => api.refundSkillRank(sheet.id, s.skillDefId))}>−</button>}
                  <button className="rd-roll" aria-label={t(`Бросить пул навыка «${label}»`, `Roll the "${label}" skill pool`)} title={t('Бросить пул', 'Roll pool')}
                    onClick={() => openRoller({ kind: 'roll', title: t('Бросок навыка', 'Skill check'), label,
                      spendContext: s.kind === 'social' ? 'social' : s.kind === 'combat' ? 'combat' : s.kind === 'magic' ? 'magic' : 'general',
                      initialPool: { ability: s.pool.ability, proficiency: s.pool.proficiency, setback: s.setbackDice, boost: s.boostDice, difficulty: s.difficultyDice ?? 0 }, difficultyUpgrades: s.difficultyUpgrades ?? 0 })}><Icon name="dice" /></button>
                  {progress && <button className="rd-buy" disabled={!canBuy} onMouseEnter={() => setPreviewSkill(s.skillDefId)} onMouseLeave={() => setPreviewSkill(null)}
                    onFocus={() => setPreviewSkill(s.skillDefId)} onBlur={() => setPreviewSkill(null)} title={unavailableReason || (s.ranks >= maxRank ? sheet.isCreationPhase ? t('При создании максимальный ранг — 2', 'Maximum rank during creation is 2') : t('Максимальный ранг', 'Maximum rank') : !canBuy ? t(`Нужно ${s.nextRankCost} XP — доступно ${sheet.availableXp}`, `Requires ${s.nextRankCost} XP — available ${sheet.availableXp}`) : t(`Купить ранг ${s.ranks + 1} за ${s.nextRankCost} XP`, `Buy rank ${s.ranks + 1} for ${s.nextRankCost} XP`))}
                    onClick={() => { setPreviewSkill(null); void run(() => api.buySkillRank(sheet.id, s.skillDefId)) }}>{s.ranks >= 5 ? t('макс', 'max') : `+${s.nextRankCost}`}</button>}
                </div>
              </div>
            })}
          </section>
        })}</div>)}</div>
      </section>

    </div>
  )
}

function readProgress(id: string, creation: boolean): boolean {
  try { const saved = localStorage.getItem(`genesysforge.skillProgress.${id}`); return saved === null ? creation : saved === 'true' } catch { return creation }
}
function statTitle(breakdown: Derived['soakBreakdown'], total: number): string | undefined {
  if (!breakdown) return undefined
  return `${t('Основа', 'Base')} ${breakdown.base}${breakdown.sources.map(x => ` + ${x.sourceName === 'Base' ? t('Базовый бонус', 'Base bonus') : x.sourceName} ${x.value}`).join('')} = ${total}`
}

/**
 * Краткая сводка героической способности на листе: базовый эффект и только уже купленные
 * улучшения. Полное описание, покупка и настройка живут на отдельной вкладке — лист не должен
 * тонуть в тексте ещё не приобретённых улучшений.
 */
function HeroicSummary({ sheet }: { sheet: CharacterSheet }) {
  const h = sheet.heroicAbility
  const upgrades = sheet.heroicUpgrades

  if (!h) {
    return (
      <section className="panel">
        <h3>{t('Героическая способность', 'Heroic ability')}</h3>
        <p className="hint">
          {t('Не выбрана — откройте вкладку «Героика».', 'Not chosen — open the “Heroic” tab.')}
        </p>
      </section>
    )
  }

  const meta = [
    upgrades.story ? t('1 очко сюжета', '1 Story Point') : h.activationCost,
    h.activation,
    upgrades.durationRanks > 0
      ? t(`${h.duration} · +${upgrades.durationRanks} ход.`, `${h.duration} · +${upgrades.durationRanks} turn(s)`)
      : h.duration,
    upgrades.frequencyRanks > 0
      ? t(`${1 + upgrades.frequencyRanks} раз за сессию`, `${1 + upgrades.frequencyRanks} times per session`)
      : h.frequency,
  ].filter(Boolean).join(' · ')

  const purchased = h.upgrades.filter(u => u.level <= upgrades.powerRank)

  return (
    <section className="panel">
      <h3>{t('Героическая способность', 'Heroic ability')}</h3>
      <div className="heroic">
        <strong>{sheet.heroicIdentity?.customName || localizedName(h)}</strong>
        {sheet.heroicIdentity?.customName && (
          <div className="hint small-text">{t('Эффект:', 'Effect:')} {localizedName(h)}</div>
        )}
        {localizedDescription(h) && <p>{localizedDescription(h)}</p>}
        <BookReference source={h.source} />
        {meta && <div className="hint small-text">{meta}</div>}

        {purchased.map(u => (
          <div key={u.level} className="heroic-upgrade bought">
            <div className="heroic-upgrade-head">
              <strong>{HEROIC_UPGRADE_LABELS[u.level] ?? t(`Уровень ${u.level}`, `Level ${u.level}`)}</strong>
            </div>
            {localizedDescription(u) && <p>{localizedDescription(u)}</p>}
            <BookReference source={u.source || h.source} />
          </div>
        ))}

        {upgrades.secondaryEffects.map(effect => (
          <div key={effect.id} className="heroic-upgrade bought">
            <div className="heroic-upgrade-head"><strong>{localizedName(effect)}</strong></div>
            {localizedDescription(effect) && <p>{localizedDescription(effect)}</p>}
            <BookReference source={effect.source} />
          </div>
        ))}

        {(sheet.heroicIdentityIncomplete || sheet.heroicConfigurationIncomplete) && (
          <p className="hint small-text">
            {t('Способность настроена не до конца — откройте вкладку «Героика».',
              'The ability is not fully set up — open the “Heroic” tab.')}
          </p>
        )}
      </div>
    </section>
  )
}

/**
 * Объяснение итоговой защиты (ROT-CMB-03): что её задало, что проигнорировано (источники
 * «получает Defense N» не складываются) и упёрлось ли значение в предел 4.
 */
function defenseTitle(d: Derived): string | undefined {
  const channel = (label: string, b: DefenseBreakdown | null) => {
    if (!b) return null
    const parts: string[] = []
    if (b.provider) parts.push(`${b.provider.sourceName} ${b.provider.value}`)
    for (const inc of b.increases) parts.push(`${inc.sourceName} ${inc.value > 0 ? '+' : ''}${inc.value}`)
    if (parts.length === 0) parts.push(t('источников нет', 'no sources'))
    const ignored = b.ignoredProviders.length > 0
      ? ` · ${t('не складывается с', 'does not stack with')} ${b.ignoredProviders.map(x => x.sourceName).join(', ')}`
      : ''
    const capped = b.capped ? ` · ${t(`предел 4 (было бы ${b.raw})`, `capped at 4 (raw ${b.raw})`)}` : ''
    return `${label}: ${parts.join(' ')} = ${b.effective}${ignored}${capped}`
  }

  const lines = [
    channel(t('Ближняя', 'Melee'), d.meleeDefenseBreakdown),
    channel(t('Дальняя', 'Ranged'), d.rangedDefenseBreakdown),
  ].filter(Boolean)
  return lines.length > 0 ? lines.join('\n') : undefined
}

/**
 * Точная цена перегруза, а не просто «перегружен» (ROT-EQP-01): сколько помех добавляется к
 * проверкам Мощи и Ловкости и во что обходятся манёвры.
 */
function encumbranceWarning(d: Derived): string | undefined {
  if (!d.encumbered) return undefined
  const e = d.encumbrance
  if (!e) return t('Перегружен!', 'Encumbered!')
  const dice = t(`Перегруз: +${e.setbackDice} помех`, `Encumbered: +${e.setbackDice} setback`)
  return e.hasFreeManoeuvre
    ? dice
    : `${dice}, ${t(`манёвр — ${e.strainPerManoeuvre} усталости`, `each manoeuvre costs ${e.strainPerManoeuvre} strain`)}`
}
