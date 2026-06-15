import { useState } from 'react'
import { api } from '../api/client'
import type { Characteristic, CharacterSheet, Reference, TalentDef } from '../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, nextRankTier, talentCost } from '../utils/labels'
import { canPurchaseTier, canRemoveTier } from '../utils/pyramid'
import { talentBonusSummary } from '../utils/talentBonuses'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const TIERS = [1, 2, 3, 4, 5] as const
// Талант Dedication не поднимает характеристику выше этого значения.
const TALENT_CHARACTERISTIC_MAX = 5

export function TalentsTab({ sheet, reference, onError, refresh }: Props) {
  const [activeTier, setActiveTier] = useState<number>(1)
  // Талант, для которого открыт выбор характеристики (Dedication).
  const [pickFor, setPickFor] = useState<TalentDef | null>(null)

  async function buy(talent: TalentDef, characteristic?: string) {
    try {
      await api.buyTalent(sheet.id, talent.id, characteristic)
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    }
  }

  // Характеристики, доступные для увеличения этим талантом:
  // нельзя повторно ту же и нельзя выше максимума.
  function grantableCharacteristics(talent: TalentDef): Characteristic[] {
    const taken = new Set(sheet.talents.find(t => t.talentDefId === talent.id)?.grantedCharacteristics ?? [])
    return CHARACTERISTICS.filter(c => !taken.has(c) && sheet.characteristics[c] < TALENT_CHARACTERISTIC_MAX)
  }

  async function confirmPick(characteristic: Characteristic) {
    const talent = pickFor
    setPickFor(null)
    if (talent) await buy(talent, characteristic)
  }

  async function refund(talent: TalentDef) {
    try {
      await api.refundTalent(sheet.id, talent.id)
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    }
  }

  const owned = new Map(sheet.talents.map(t => [t.talentDefId, t]))

  // Раскладываем каждый ранг купленного таланта по тиру, который он занимает в пирамиде.
  type PyramidCard = {
    key: string
    name: string
    description: string
    activation: string
    isRanked: boolean
    rankNo?: number
    bonuses: string[]
    grant?: Characteristic
  }
  const cardsByTier = new Map<number, PyramidCard[]>(TIERS.map(t => [t, []]))
  for (const t of sheet.talents) {
    const base = reference.talents.find(d => d.id === t.talentDefId)?.tier ?? t.tier
    for (let r = 0; r < t.ranks; r++) {
      const tier = Math.min(base + r, 5)
      cardsByTier.get(tier)!.push({
        key: `${t.talentDefId}-${r}`,
        name: t.nameRu || t.name,
        description: t.description,
        activation: t.activation,
        isRanked: t.isRanked,
        rankNo: t.isRanked ? r + 1 : undefined,
        // накопленный бонус к этому рангу включительно
        bonuses: talentBonusSummary(t, r + 1),
        // характеристика, выбранная для этого ранга (Dedication)
        grant: t.grantsCharacteristic ? t.grantedCharacteristics[r] : undefined,
      })
    }
  }

  const tierTalents = reference.talents.filter(t => t.tier === activeTier)

  return (
    <div>
      <section className="panel">
        <h3>Пирамида талантов</h3>
        <p className="hint">
          Талантов каждого тира должно быть строго больше, чем талантов тира выше.
          Стоимость — тир × 5 XP. Ранговые таланты: каждый следующий ранг покупается на тир выше.
        </p>
        {sheet.talents.length === 0 && (
          <p className="muted">Пока нет талантов — купите первый из списка ниже.</p>
        )}
        <div className="pyramid">
          {TIERS.map(tier => {
            const slots = sheet.talentTierCounts[String(tier)] ?? 0
            const cards = cardsByTier.get(tier)!
            return (
              <div key={tier} className="pyramid-tier">
                <div className="tier-label">Тир {tier} <span className="muted">({slots})</span></div>
                <div className="tier-cells">
                  {cards.map(c => {
                    const isPassive = c.activation.toLowerCase().startsWith('пассив')
                    return (
                      <div key={c.key} className="pyramid-card">
                        <div className="owned-talent-head">
                          <strong>{c.name}</strong>
                          {c.rankNo && <span className="badge">ранг {c.rankNo}</span>}
                          <span className={isPassive ? 'badge passive' : 'badge active'}>
                            {isPassive ? 'Пассивный' : `Активный · ${c.activation}`}
                          </span>
                        </div>
                        <p className="owned-talent-desc">{c.description}</p>
                        {c.grant && (
                          <div className="bonus-line" title="Характеристика увеличена этим талантом">
                            ⬆ +1 {CHARACTERISTIC_LABELS[c.grant]}
                          </div>
                        )}
                        {c.bonuses.length > 0 && (
                          <div className="bonus-line" title="Применяется к производным характеристикам автоматически">
                            ⚡ {c.bonuses.join(' · ')}
                          </div>
                        )}
                      </div>
                    )
                  })}
                  {cards.length === 0 && <div className="pyramid-card empty">—</div>}
                </div>
              </div>
            )
          })}
        </div>
      </section>

      <section className="panel">
        <h3>Доступные таланты</h3>
        <div className="tabs">
          {TIERS.map(tier => {
            const count = reference.talents.filter(t => t.tier === tier).length
            return (
              <button key={tier}
                className={activeTier === tier ? 'tab active' : 'tab'}
                onClick={() => setActiveTier(tier)}>
                Тир {tier} <span className="muted">({count})</span>
              </button>
            )
          })}
        </div>
        <div className="talent-list">
          {tierTalents.length === 0 && (
            <p className="muted">Нет талантов этого тира.</p>
          )}
          {tierTalents.map(t => {
            const ownedRow = owned.get(t.id)
            const ranksOwned = ownedRow?.ranks ?? 0
            const maxedOut = ownedRow && !t.isRanked
            const effectiveTier = nextRankTier(t.tier, ranksOwned)
            const cost = talentCost(effectiveTier)
            const pyramidOk = canPurchaseTier(sheet.talentTierCounts, effectiveTier)
            const affordable = cost <= sheet.availableXp
            const noGrantsLeft = t.grantsCharacteristic && grantableCharacteristics(t).length === 0
            const reason = maxedOut ? 'Уже куплен'
              : !pyramidOk ? 'Нарушит пирамиду'
              : !affordable ? 'Недостаточно XP'
              : noGrantsLeft ? 'Нет характеристик для увеличения'
              : null
            return (
              <div key={t.id} className="talent-row">
                <div className="talent-info">
                  <strong>
                    {t.nameRu || t.name} <span className="badge tier">Тир {t.tier}</span>
                    {t.isRanked && <span className="badge">Ранговый{ranksOwned > 0 ? `: ${ranksOwned}` : ''}</span>}
                    {t.isCustom && <span className="badge custom">Кастом</span>}
                    <span className="muted"> · {t.activation}</span>
                  </strong>
                  <p className="muted">{t.description}</p>
                </div>
                <div className="talent-actions">
                  {sheet.isCreationPhase && ranksOwned > 0 && (() => {
                    const lastTier = nextRankTier(t.tier, ranksOwned - 1)
                    const removable = canRemoveTier(sheet.talentTierCounts, lastTier)
                    return (
                      <button className="small" disabled={!removable}
                        title={removable
                          ? `Вернуть ${t.isRanked ? `ранг ${ranksOwned}` : 'талант'} (+${talentCost(lastTier)} XP)`
                          : 'Нельзя вернуть: нарушится пирамида'}
                        onClick={() => refund(t)}>
                        Вернуть
                      </button>
                    )
                  })()}
                  <button className="small" disabled={!!reason} title={reason ?? ''}
                    onClick={() => (t.grantsCharacteristic ? setPickFor(t) : buy(t))}>
                    {maxedOut ? 'Куплен' : `Купить (${cost} XP${t.isRanked && ranksOwned > 0 ? `, тир ${effectiveTier}` : ''})`}
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      </section>

      {pickFor && (
        <div className="modal-backdrop" onClick={() => setPickFor(null)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <h3>{pickFor.nameRu || pickFor.name}: выбор характеристики</h3>
            <p className="hint">Талант увеличивает выбранную характеристику на 1 (не выше {TALENT_CHARACTERISTIC_MAX}).</p>
            <div className="chips">
              {grantableCharacteristics(pickFor).map(c => (
                <button key={c} type="button" className="chip"
                  onClick={() => confirmPick(c)}>
                  {CHARACTERISTIC_LABELS[c]} <span className="muted">{sheet.characteristics[c]} → {sheet.characteristics[c] + 1}</span>
                </button>
              ))}
            </div>
            <div className="modal-actions">
              <button type="button" onClick={() => setPickFor(null)}>Отмена</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
