import { useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, Reference, TalentDef } from '../api/types'
import { nextRankTier, talentCost } from '../utils/labels'
import { canPurchaseTier, canRemoveTier } from '../utils/pyramid'
import { talentBonusSummary } from '../utils/talentBonuses'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const TIERS = [1, 2, 3, 4, 5] as const

export function TalentsTab({ sheet, reference, onError, refresh }: Props) {
  const [activeTier, setActiveTier] = useState<number>(1)

  async function buy(talent: TalentDef) {
    try {
      await api.buyTalent(sheet.id, talent.id)
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    }
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
            const reason = maxedOut ? 'Уже куплен'
              : !pyramidOk ? 'Нарушит пирамиду'
              : !affordable ? 'Недостаточно XP'
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
                    onClick={() => buy(t)}>
                    {maxedOut ? 'Куплен' : `Купить (${cost} XP${t.isRanked && ranksOwned > 0 ? `, тир ${effectiveTier}` : ''})`}
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      </section>
    </div>
  )
}
