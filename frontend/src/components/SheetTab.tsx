import { useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, Reference, SkillKind } from '../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, CHARACTERISTIC_SHORT_LABELS, SKILL_KIND_LABELS } from '../utils/labels'
import { DicePoolView } from './DicePoolView'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const SKILL_KINDS: SkillKind[] = ['general', 'combat', 'social', 'knowledge', 'magic']

export function SheetTab({ sheet, reference, onError, refresh }: Props) {
  const [heroicPick, setHeroicPick] = useState('')

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
              <button className="small" title={`Повысить за ${(sheet.characteristics[c] + 1) * 10} XP`}
                onClick={() => run(() => api.buyCharacteristic(sheet.id, c))}>
                +{(sheet.characteristics[c] + 1) * 10} XP
              </button>
            )}
          </div>
        ))}
      </section>

      <section className="stat-row derived">
        <DerivedBox label="Раны (HP)" value={`${sheet.woundsCurrent} / ${d.woundThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent + 1 }))} />
        <DerivedBox label="Стрейн (стамина)" value={`${sheet.strainCurrent} / ${d.strainThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent + 1 }))} />
        <DerivedBox label="Поглощение" value={String(d.soak)} />
        <DerivedBox label="Защита (ближ/дальн)" value={`${d.meleeDefense} / ${d.rangedDefense}`} />
        <DerivedBox label="Переносимый вес" value={`${d.encumbranceLoad} / ${d.encumbranceThreshold}`}
          warning={d.encumbered ? 'Перегружен!' : undefined} />
      </section>

      {sheet.system === 'realmsOfTerrinoth' && (
        <section className="panel">
          <h3>Героическая способность</h3>
          {sheet.heroicAbility ? (
            <div className="heroic">
              <strong>{sheet.heroicAbility.name}</strong>
              <p>{sheet.heroicAbility.description}</p>
              <button className="small" onClick={() => run(() => api.setHeroicAbility(sheet.id, null))}>Сбросить</button>
            </div>
          ) : (
            <div className="inline-form">
              <select value={heroicPick} onChange={e => setHeroicPick(e.target.value)}>
                <option value="" disabled>— выберите способность —</option>
                {reference.heroicAbilities.map(h => (
                  <option key={h.id} value={h.id}>{h.name}{h.isCustom ? ' (кастом)' : ''}</option>
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
          {SKILL_KINDS.map(kind => {
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
                      <th>Дайс-пул</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {skills.map(s => (
                      <tr key={s.skillDefId}>
                        <td className="ellipsis" title={s.name}>{s.name}</td>
                        <td className="muted" title={CHARACTERISTIC_LABELS[s.characteristic]}>
                          {CHARACTERISTIC_SHORT_LABELS[s.characteristic]}
                        </td>
                        <td className="centered">{s.isCareer ? '✓' : ''}</td>
                        <td>{'●'.repeat(s.ranks)}{'○'.repeat(Math.max(0, 5 - s.ranks))}</td>
                        <td><DicePoolView pool={s.pool} /></td>
                        <td className="right">
                          {s.ranks < 5 && (
                            <button className="small" disabled={s.nextRankCost > sheet.availableXp}
                              title={s.nextRankCost > sheet.availableXp ? 'Недостаточно XP' : `Купить ранг ${s.ranks + 1}`}
                              onClick={() => run(() => api.buySkillRank(sheet.id, s.skillDefId))}>
                              +{s.nextRankCost} XP
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          })}
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
