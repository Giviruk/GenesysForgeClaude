import { useState } from 'react'
import { api } from '../api/client'
import type { ActivateCharacterAbilityResult, CharacterSheet, Reference, SkillKind } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, CHARACTERISTIC_SHORT_LABELS, localizedName, SKILL_KIND_LABELS,
} from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { CriticalInjuriesSection } from './CriticalInjuriesSection'
import { useDiceRoller } from '../dice-roller-store'

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
      onError(err instanceof Error ? err.message : 'Ошибка')
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
                  <button className="small" title={`Вернуть ${sheet.characteristics[c] * 10} XP`}
                    onClick={() => run(() => api.refundCharacteristic(sheet.id, c))}>
                    −
                  </button>
                )}
                <button className="small" title={`Повысить за ${(sheet.characteristics[c] + 1) * 10} XP`}
                  onClick={() => run(() => api.buyCharacteristic(sheet.id, c))}>
                  +{(sheet.characteristics[c] + 1) * 10} XP
                </button>
              </div>
            )}
          </div>
        ))}
      </section>

      <section className="stat-row derived">
        <DerivedBox label="Раны" value={`${sheet.woundsCurrent} / ${d.woundThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent + 1 }))} />
        <DerivedBox label="Усталость" value={`${sheet.strainCurrent} / ${d.strainThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent + 1 }))} />
        <DerivedBox label="Поглощение" value={String(d.soak)} />
        <DerivedBox label="Защита (ближ/дальн)" value={`${d.meleeDefense} / ${d.rangedDefense}`} />
        <DerivedBox label="Переносимый вес" value={`${d.encumbranceLoad} / ${d.encumbranceThreshold}`}
          warning={d.encumbered ? 'Перегружен!' : undefined} />
      </section>

      <CriticalInjuriesSection sheet={sheet} onError={onError} refresh={refresh} />

      {sheet.system === 'realmsOfTerrinoth' && (
        <section className="panel">
          <h3>Героическая способность</h3>
          {sheet.heroicAbility ? (
            <HeroicAbilityCard sheet={sheet} run={run} />
          ) : (
            <div className="inline-form">
              <select value={heroicPick} onChange={e => setHeroicPick(e.target.value)}>
                <option value="" disabled>— выберите способность —</option>
                {reference.heroicAbilities.map(h => (
                  <option key={h.id} value={h.id}>{h.nameRu || h.name}{h.isCustom ? ' (кастом)' : ''}</option>
                ))}
              </select>
              <button className="primary" disabled={!heroicPick}
                onClick={() => run(() => api.setHeroicAbility(sheet.id, heroicPick))}>
                Выбрать
              </button>
            </div>
          )}
          {heroicPick && !sheet.heroicAbility && (
            <p className="hint">{reference.heroicAbilities.find(h => h.id === heroicPick)?.description}</p>
          )}
        </section>
      )}

      <section className="panel">
        <h3>Навыки</h3>
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
                          <th>Навык</th>
                          <th>Хар-ка</th>
                          <th className="centered" title="Карьерный навык">Карьерн.</th>
                          <th>Ранги</th>
                          <th>Пул кубов</th>
                          <th></th>
                        </tr>
                      </thead>
                      <tbody>
                        {skills.map(s => {
                          const label = localizedName(s)
                          return (
                            <tr key={s.skillDefId}>
                              <td className="ellipsis" title={label}>{label}</td>
                              <td className="muted" title={CHARACTERISTIC_LABELS[s.characteristic]}>
                                {CHARACTERISTIC_SHORT_LABELS[s.characteristic]}
                              </td>
                              <td className="centered">{s.isCareer ? '✓' : ''}</td>
                              <td>{'●'.repeat(s.ranks)}{'○'.repeat(Math.max(0, 5 - s.ranks))}</td>
                              <td><DicePoolView pool={s.pool} /></td>
                              <td className="right">
                                <button className="small" title={`Бросить пул навыка «${label}»`}
                                  onClick={() => openRoller({
                                    kind: 'roll',
                                    title: 'Бросок навыка',
                                    label,
                                    initialPool: { ability: s.pool.ability, proficiency: s.pool.proficiency },
                                  })}>
                                  🎲
                                </button>
                                {sheet.isCreationPhase && s.ranks > s.freeRanks && (
                                  <button className="small"
                                    title={`Вернуть ранг ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`}
                                    onClick={() => run(() => api.refundSkillRank(sheet.id, s.skillDefId))}>
                                    −
                                  </button>
                                )}
                                {s.ranks < 5 && (
                                  <button className="small" disabled={s.nextRankCost > sheet.availableXp}
                                    title={s.nextRankCost > sheet.availableXp ? 'Недостаточно XP' : `Купить ранг ${s.ranks + 1}`}
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

const UPGRADE_LABELS: Record<number, string> = { 1: 'Улучшенная', 2: 'Высшая' }

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
    ['Активация', [h.activationCost, h.activation].filter(Boolean).join(' · ')],
    ['Длительность', h.duration],
    ['Частота', h.frequency],
    ['Требование', h.requirement && h.requirement !== '—' ? h.requirement : ''],
  ]

  return (
    <div className="heroic">
      <strong>{h.nameRu || h.name}</strong>
      <p>{h.description}</p>
      {meta.filter(([, v]) => v).map(([k, v]) => (
        <div key={k} className="hint small-text"><b>{k}:</b> {v}</div>
      ))}
      {h.notes && <p className="hint small-text">{h.notes}</p>}

      {h.upgrades.length > 0 && (
        <div className="heroic-upgrades">
          <div className="label-line">
            Улучшения · очков доступно: {available} из {total}
            <span className="hint"> (1 стартовое + по 1 каждые 50 заработанного XP)</span>
          </div>
          {h.upgrades.map(u => {
            const purchased = rank >= u.level
            const isNext = u.level === rank + 1
            const canBuy = isNext && available >= u.cost
            const isTop = purchased && u.level === rank
            return (
              <div key={u.level} className={purchased ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
                <div className="heroic-upgrade-head">
                  <strong>{UPGRADE_LABELS[u.level] ?? `Уровень ${u.level}`}</strong>
                  <span className="hint"> · {u.cost} очк.</span>
                  {purchased && <span className="badge"> куплено</span>}
                  {!purchased && canBuy && (
                    <button className="small primary"
                      onClick={() => run(() => api.setHeroicUpgradeRank(sheet.id, u.level))}>
                      Купить
                    </button>
                  )}
                  {!purchased && isNext && !canBuy && <span className="hint"> — не хватает очков</span>}
                  {!purchased && !isNext && <span className="hint"> — сначала купите предыдущее</span>}
                  {isTop && (
                    <button className="small"
                      onClick={() => run(() => api.setHeroicUpgradeRank(sheet.id, u.level - 1))}>
                      Вернуть
                    </button>
                  )}
                </div>
                <p>{u.description}</p>
                {u.notes && <p className="hint small-text">{u.notes}</p>}
              </div>
            )
          })}
        </div>
      )}

      {h.effects.length > 0 && (
        <div className="heroic-activate">
          <button className="small primary" onClick={() => void activate()}>🎯 Активировать</button>
          {outcome && (
            <div className="heroic-activate-result small-text">
              {outcome.applied.map((a, i) => <div key={`a${i}`}>{a}</div>)}
              {outcome.manual.map((m, i) => <div key={`m${i}`} className="muted">{m}</div>)}
            </div>
          )}
        </div>
      )}

      <button className="small" onClick={() => run(() => api.setHeroicAbility(sheet.id, null))}>
        Сбросить способность
      </button>
    </div>
  )
}
