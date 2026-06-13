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

export function TalentsTab({ sheet, reference, onError, refresh }: Props) {
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

  return (
    <div>
      <section className="panel">
        <h3>Пирамида талантов</h3>
        <p className="hint">
          Талантов каждого тира должно быть строго больше, чем талантов тира выше.
          Стоимость — тир × 5 XP. Ранговые таланты: каждый следующий ранг покупается на тир выше.
        </p>
        <div className="pyramid">
          {[1, 2, 3, 4, 5].map(tier => {
            const slots = sheet.talentTierCounts[String(tier)] ?? 0
            const rows: { name: string; rank?: number; description: string }[] = []
            for (const t of sheet.talents) {
              const base = reference.talents.find(d => d.id === t.talentDefId)?.tier ?? t.tier
              for (let r = 0; r < t.ranks; r++) {
                if (Math.min(base + r, 5) === tier) {
                  rows.push({ name: t.name, rank: t.isRanked ? r + 1 : undefined, description: t.description })
                }
              }
            }
            return (
              <div key={tier} className="pyramid-tier">
                <div className="tier-label">Тир {tier} <span className="muted">({slots})</span></div>
                <div className="tier-cells">
                  {rows.map((r, i) => (
                    <div key={i} className="talent-cell"
                      title={`${r.name}${r.rank ? ` — ранг ${r.rank}` : ''}\n${r.description}`}>
                      {r.name}{r.rank ? ` ${r.rank}` : ''}
                    </div>
                  ))}
                  {rows.length === 0 && <div className="talent-cell empty">—</div>}
                </div>
              </div>
            )
          })}
        </div>
      </section>

      <section className="panel">
        <h3>Купленные таланты</h3>
        {sheet.talents.length === 0 && (
          <p className="muted">Пока нет талантов — купите первый из списка ниже.</p>
        )}
        <div className="owned-talents">
          {sheet.talents.map(t => {
            const bonuses = talentBonusSummary(t, t.ranks)
            const isPassive = t.activation.toLowerCase().startsWith('пассив')
            return (
              <div key={t.talentDefId} className="owned-talent">
                <div className="owned-talent-head">
                  <strong>{t.name}</strong>
                  <span className="badge tier">Тир {t.tier}</span>
                  <span className={isPassive ? 'badge passive' : 'badge active'}>
                    {isPassive ? 'Пассивный' : `Активный · ${t.activation}`}
                  </span>
                </div>
                <div className="owned-talent-ranks">
                  {t.isRanked
                    ? (() => {
                        const maxRanks = 6 - t.tier // каждый ранг толкает эффективный тир к 5
                        return <>Ранги: {'●'.repeat(t.ranks)}{'○'.repeat(Math.max(0, maxRanks - t.ranks))} <span className="muted">({t.ranks}/{maxRanks})</span></>
                      })()
                    : <span className="muted">Одноранговый талант</span>}
                </div>
                <p className="owned-talent-desc">{t.description}</p>
                {bonuses.length > 0 && (
                  <div className="bonus-line" title="Применяется к производным характеристикам автоматически">
                    ⚡ {bonuses.join(' · ')}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </section>

      <section className="panel">
        <h3>Доступные таланты</h3>
        <div className="talent-list">
          {reference.talents.map(t => {
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
                    {t.name} <span className="badge tier">Тир {t.tier}</span>
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
