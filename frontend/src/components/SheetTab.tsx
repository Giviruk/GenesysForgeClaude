import { useState } from 'react'
import { api } from '../api/client'
import type { ActivateCharacterAbilityResult, CharacterSheet, Reference, SkillKind } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, CHARACTERISTIC_SHORT_LABELS, localizedDescription, localizedName,
  secondaryName, SKILL_KIND_LABELS,
} from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { CriticalInjuriesSection } from './CriticalInjuriesSection'
import { useDiceRoller } from '../dice-roller-store'
import { t } from '../i18n'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

// Левая колонка — крупный блок «общие»; правая — боевые, под ними знания/магия и
// социальные, чтобы плотно заполнить пространство и меньше скроллить.
const SKILL_COLUMNS: SkillKind[][] = [
  ['general'],
  ['combat', 'knowledge', 'magic', 'social'],
]

export function SheetTab({ sheet, reference, onError, refresh }: Props) {
  const [heroicPick, setHeroicPick] = useState('')
  const { openRoller } = useDiceRoller()

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  const d = sheet.derived

  return (
    <div>
      <section className="stat-row">
        {CHARACTERISTICS.map(c => (
          <div key={c} className="stat-box characteristic">
            <div className="stat-value">{sheet.characteristics[c]}</div>
            <div className="stat-label">{CHARACTERISTIC_LABELS[c]}</div>
            {sheet.isCreationPhase && (
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

      <section className="stat-row derived">
        <DerivedBox label={t('Раны', 'Wounds')} value={`${sheet.woundsCurrent} / ${d.woundThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent + 1 }))} />
        <DerivedBox label={t('Усталость', 'Strain')} value={`${sheet.strainCurrent} / ${d.strainThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent + 1 }))} />
        <DerivedBox label={t('Поглощение', 'Soak')} value={String(d.soak)} />
        <DerivedBox label={t('Защита (ближ/дальн)', 'Defense (melee/ranged)')} value={`${d.meleeDefense} / ${d.rangedDefense}`} />
        <DerivedBox label={t('Переносимый вес', 'Encumbrance')} value={`${d.encumbranceLoad} / ${d.encumbranceThreshold}`}
          warning={d.encumbered ? t('Перегружен!', 'Encumbered!') : undefined} />
      </section>

      <CriticalInjuriesSection sheet={sheet} onError={onError} refresh={refresh} />

      {sheet.system === 'realmsOfTerrinoth' && (
        <section className="panel">
          <h3>{t('Героическая способность', 'Heroic ability')}</h3>
          {sheet.heroicAbility ? (
            <HeroicAbilityCard sheet={sheet} run={run} />
          ) : (
            <div className="inline-form">
              <select value={heroicPick} onChange={e => setHeroicPick(e.target.value)}>
                <option value="" disabled>{t('— выберите способность —', '— pick an ability —')}</option>
                {reference.heroicAbilities.map(h => (
                  <option key={h.id} value={h.id}>{localizedName(h)}{h.isCustom ? t(' (кастом)', ' (custom)') : ''}</option>
                ))}
              </select>
              <button className="primary" disabled={!heroicPick}
                onClick={() => run(() => api.setHeroicAbility(sheet.id, heroicPick))}>
                {t('Выбрать', 'Choose')}
              </button>
            </div>
          )}
          {heroicPick && !sheet.heroicAbility && (
            <p className="hint">{(() => { const h = reference.heroicAbilities.find(x => x.id === heroicPick); return h ? localizedDescription(h) : '' })()}</p>
          )}
        </section>
      )}

      <section className="panel">
        <h3>{t('Навыки', 'Skills')}</h3>
        <div className="skills-grid">
          {SKILL_COLUMNS.map((kinds, i) => (
            <div key={i} className="skill-column">
              {kinds.map(kind => {
                const skills = sheet.skills.filter(s => s.kind === kind)
                if (skills.length === 0) return null
                return (
                  <div key={kind} className="skill-block">
                    <h4 className="skill-kind">{SKILL_KIND_LABELS[kind]}</h4>
                    <table className="skills fixed">
                      {/* единые ширины колонок во всех разделах */}
                      <colgroup>
                        <col className="col-name" />
                        <col className="col-char" />
                        <col className="col-career" />
                        <col className="col-ranks" />
                        <col className="col-pool" />
                        <col className="col-action" />
                      </colgroup>
                      <thead>
                        <tr>
                          <th>{t('Навык', 'Skill')}</th>
                          <th>{t('Хар-ка', 'Char.')}</th>
                          <th className="centered" title={t('Карьерный навык', 'Career skill')}>{t('Карьерн.', 'Career')}</th>
                          <th>{t('Ранги', 'Ranks')}</th>
                          <th>{t('Пул кубов', 'Dice pool')}</th>
                          <th></th>
                        </tr>
                      </thead>
                      <tbody>
                        {skills.map(s => {
                          const label = localizedName(s)
                          const original = secondaryName(s)
                          return (
                            <tr key={s.skillDefId}>
                              <td className="ellipsis" title={original ? `${label} / ${original}` : label}>
                                {label}
                                {original && <span className="muted small-text name-secondary"> · {original}</span>}
                              </td>
                              <td className="muted" title={CHARACTERISTIC_LABELS[s.characteristic]}>
                                {CHARACTERISTIC_SHORT_LABELS[s.characteristic]}
                              </td>
                              <td className="centered">{s.isCareer ? '✓' : ''}</td>
                              <td>{'●'.repeat(s.ranks)}{'○'.repeat(Math.max(0, 5 - s.ranks))}</td>
                              <td><DicePoolView pool={s.pool} /></td>
                              <td className="right">
                                <button className="small" title={t(`Бросить пул навыка «${label}»`, `Roll the "${label}" skill pool`)}
                                  onClick={() => openRoller({
                                    kind: 'roll',
                                    title: t('Бросок навыка', 'Skill check'),
                                    label,
                                    initialPool: { ability: s.pool.ability, proficiency: s.pool.proficiency },
                                  })}>
                                  🎲
                                </button>
                                {sheet.isCreationPhase && s.ranks > s.freeRanks && (
                                  <button className="small"
                                    title={t(`Вернуть ранг ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`, `Refund rank ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`)}
                                    onClick={() => run(() => api.refundSkillRank(sheet.id, s.skillDefId))}>
                                    −
                                  </button>
                                )}
                                {s.ranks < 5 && (
                                  <button className="small" disabled={s.nextRankCost > sheet.availableXp}
                                    title={s.nextRankCost > sheet.availableXp ? t('Недостаточно XP', 'Not enough XP') : t(`Купить ранг ${s.ranks + 1}`, `Buy rank ${s.ranks + 1}`)}
                                    onClick={() => run(() => api.buySkillRank(sheet.id, s.skillDefId))}>
                                    +{s.nextRankCost} XP
                                  </button>
                                )}
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  </div>
                )
              })}
            </div>
          ))}
        </div>
      </section>

    </div>
  )
}

function DerivedBox({ label, value, warning, onMinus, onPlus }: {
  label: string
  value: string
  warning?: string
  onMinus?: () => void
  onPlus?: () => void
}) {
  return (
    <div className={warning ? 'stat-box warn' : 'stat-box'}>
      <div className="stat-value">
        {onMinus && <button className="tiny" onClick={onMinus}>−</button>}
        <span>{value}</span>
        {onPlus && <button className="tiny" onClick={onPlus}>+</button>}
      </div>
      <div className="stat-label">{label}</div>
      {warning && <div className="error small-text">{warning}</div>}
    </div>
  )
}

const UPGRADE_LABELS: Record<number, string> = t({ 1: 'Улучшенная', 2: 'Высшая' }, { 1: 'Improved', 2: 'Supreme' })

function HeroicAbilityCard({ sheet, run }: {
  sheet: CharacterSheet
  run: (action: () => Promise<unknown>) => Promise<void>
}) {
  const h = sheet.heroicAbility!
  const rank = sheet.heroicUpgradeRank
  const total = sheet.heroicUpgradePointsTotal
  const available = total - sheet.heroicUpgradePointsSpent
  const [outcome, setOutcome] = useState<ActivateCharacterAbilityResult | null>(null)

  async function activate() {
    await run(async () => { setOutcome(await api.activateCharacterAbility(sheet.id)) })
  }
  const meta: [string, string][] = [
    [t('Активация', 'Activation'), [h.activationCost, h.activation].filter(Boolean).join(' · ')],
    [t('Длительность', 'Duration'), h.duration],
    [t('Частота', 'Frequency'), h.frequency],
    [t('Требование', 'Requirement'), h.requirement && h.requirement !== '—' ? h.requirement : ''],
  ]

  return (
    <div className="heroic">
      <strong>{localizedName(h)}</strong>
      <p>{localizedDescription(h)}</p>
      {meta.filter(([, v]) => v).map(([k, v]) => (
        <div key={k} className="hint small-text"><b>{k}:</b> {v}</div>
      ))}
      {h.notes && <p className="hint small-text">{h.notes}</p>}

      {h.upgrades.length > 0 && (
        <div className="heroic-upgrades">
          <div className="label-line">
            {t(`Улучшения · очков доступно: ${available} из ${total}`, `Upgrades · points available: ${available} of ${total}`)}
            <span className="hint"> {t('(1 стартовое + по 1 каждые 50 заработанного XP)', '(1 to start + 1 per 50 earned XP)')}</span>
          </div>
          {h.upgrades.map(u => {
            const purchased = rank >= u.level
            const isNext = u.level === rank + 1
            const canBuy = isNext && available >= u.cost
            const isTop = purchased && u.level === rank
            return (
              <div key={u.level} className={purchased ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
                <div className="heroic-upgrade-head">
                  <strong>{UPGRADE_LABELS[u.level] ?? t(`Уровень ${u.level}`, `Level ${u.level}`)}</strong>
                  <span className="hint"> · {u.cost} {t('очк.', 'pts')}</span>
                  {purchased && <span className="badge"> {t('куплено', 'purchased')}</span>}
                  {!purchased && canBuy && (
                    <button className="small primary"
                      onClick={() => run(() => api.setHeroicUpgradeRank(sheet.id, u.level))}>
                      {t('Купить', 'Buy')}
                    </button>
                  )}
                  {!purchased && isNext && !canBuy && <span className="hint"> {t('— не хватает очков', '— not enough points')}</span>}
                  {!purchased && !isNext && <span className="hint"> {t('— сначала купите предыдущее', '— buy the previous one first')}</span>}
                  {isTop && (
                    <button className="small"
                      onClick={() => run(() => api.setHeroicUpgradeRank(sheet.id, u.level - 1))}>
                      {t('Вернуть', 'Refund')}
                    </button>
                  )}
                </div>
                <p>{localizedDescription(u)}</p>
                {u.notes && <p className="hint small-text">{u.notes}</p>}
              </div>
            )
          })}
        </div>
      )}

      {h.effects.length > 0 && (
        <div className="heroic-activate">
          <button className="small primary" onClick={() => void activate()}>{t('🎯 Активировать', '🎯 Activate')}</button>
          {outcome && (
            <div className="heroic-activate-result small-text">
              {outcome.applied.map((a, i) => <div key={`a${i}`}>{a}</div>)}
              {outcome.manual.map((m, i) => <div key={`m${i}`} className="muted">{m}</div>)}
            </div>
          )}
        </div>
      )}

      <button className="small" onClick={() => run(() => api.setHeroicAbility(sheet.id, null))}>
        {t('Сбросить способность', 'Reset ability')}
      </button>
    </div>
  )
}
