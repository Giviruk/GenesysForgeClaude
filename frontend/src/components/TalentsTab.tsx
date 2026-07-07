import { useState } from 'react'
import { api } from '../api/client'
import type { Characteristic, CharacterSheet, Reference, SheetTalent, TalentCategory, TalentDef } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, localizedName, nextRankTier, secondaryName,
  TALENT_CATEGORIES, TALENT_CATEGORY_LABELS, talentCost,
} from '../utils/labels'
import { canPurchaseTier, canRemoveTier } from '../utils/pyramid'
import { talentBonusSummary } from '../utils/talentBonuses'
import { Icon } from './Icon'
import { PrintPreview } from './print/PrintPreview'
import { TalentCard } from './print/cards'

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
  const [activeTier, setActiveTier] = useState<number | 'all'>('all')
  const [categoryFilter, setCategoryFilter] = useState<TalentCategory | 'all'>('all')
  // Талант, для которого открыт выбор характеристики (Dedication).
  const [pickFor, setPickFor] = useState<TalentDef | null>(null)
  // Купленный талант, открытый на печать.
  const [printTalent, setPrintTalent] = useState<SheetTalent | null>(null)

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
        name: localizedName(t),
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

  const matchesCategory = (talent: TalentDef) => categoryFilter === 'all' || talent.category === categoryFilter
  const matchesTier = (talent: TalentDef) => activeTier === 'all' || talent.tier === activeTier
  const visibleTalents = reference.talents
    .filter(t => matchesCategory(t) && matchesTier(t))
    .toSorted((a, b) => {
      const byCategory = TALENT_CATEGORIES.indexOf(a.category) - TALENT_CATEGORIES.indexOf(b.category)
      if (byCategory !== 0) return byCategory
      if (a.tier !== b.tier) return a.tier - b.tier
      return localizedName(a).localeCompare(localizedName(b), 'ru')
    })

  const talentGroups = visibleTalents.reduce<Array<{ key: string; label: string; talents: TalentDef[] }>>((groups, talent) => {
    const key = `${talent.category}-${talent.tier}`
    const last = groups[groups.length - 1]
    if (last?.key === key) {
      last.talents.push(talent)
      return groups
    }
    groups.push({
      key,
      label: `${TALENT_CATEGORY_LABELS[talent.category]} · Тир ${talent.tier}`,
      talents: [talent],
    })
    return groups
  }, [])

  if (printTalent) {
    return (
      <PrintPreview title={`Талант — ${printTalent.name}`} onClose={() => setPrintTalent(null)}>
        {() => <TalentCard talent={printTalent} />}
      </PrintPreview>
    )
  }

  return (
    <div>
      <section className="panel">
        <div className="section-title-line">
          <h3>Пирамида талантов</h3>
        </div>
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
        <div className="section-title-line">
          <h3>Купленные таланты</h3>
        </div>
        {sheet.talents.length === 0 ? (
          <p className="muted">Купленных талантов пока нет.</p>
        ) : (
          <div className="owned-talents">
            {sheet.talents
              .toSorted((a, b) => a.tier - b.tier || localizedName(a).localeCompare(localizedName(b), 'ru'))
              .map(t => {
                const isPassive = t.activation.toLowerCase().startsWith('пассив')
                const bonuses = talentBonusSummary(t, t.ranks)
                return (
                  <div key={t.talentDefId} className="owned-talent">
                    <div className="owned-talent-head">
                      <strong>{localizedName(t)}</strong>
                      <span className="badge tier">Тир {t.tier}</span>
                      {t.isRanked && <span className="badge">Ранги: {t.ranks}</span>}
                      <span className={isPassive ? 'badge passive' : 'badge active'}>
                        {isPassive ? 'Пассивный' : t.activation}
                      </span>
                    </div>
                    <p className="owned-talent-desc muted">{t.description}</p>
                    {bonuses.length > 0 && <div className="bonus-line">{bonuses.join(' · ')}</div>}
                  </div>
                )
              })}
          </div>
        )}
      </section>

      <section className="panel">
        <div className="section-title-line">
          <h3>Доступные таланты</h3>
          <span className="section-count">{visibleTalents.length} из {reference.talents.length}</span>
        </div>
        <div className="tabs">
          <button className={categoryFilter === 'all' ? 'tab active' : 'tab'} onClick={() => setCategoryFilter('all')}>
            Все <span className="muted">({reference.talents.length})</span>
          </button>
          {TALENT_CATEGORIES.map(category => {
            const count = reference.talents.filter(t => t.category === category).length
            return (
              <button key={category}
                className={categoryFilter === category ? 'tab active' : 'tab'}
                onClick={() => setCategoryFilter(category)}>
                {TALENT_CATEGORY_LABELS[category]} <span className="muted">({count})</span>
              </button>
            )
          })}
        </div>
        <div className="tabs">
          <button
            className={activeTier === 'all' ? 'tab active' : 'tab'}
            onClick={() => setActiveTier('all')}>
            Все тиры <span className="muted">({reference.talents.filter(matchesCategory).length})</span>
          </button>
          {TIERS.map(tier => {
            const count = reference.talents.filter(t => t.tier === tier && matchesCategory(t)).length
            return (
              <button key={tier}
                className={activeTier === tier ? 'tab active' : 'tab'}
                onClick={() => setActiveTier(tier)}>
                Тир {tier} <span className="muted">({count})</span>
              </button>
            )
          })}
        </div>
        <div className="talent-list grouped">
          {visibleTalents.length === 0 && (
            <p className="muted">Нет талантов в выбранной категории и тире.</p>
          )}
          {talentGroups.map(group => (
            <div key={group.key} className="talent-group">
              <div className="talent-group-title">{group.label}</div>
              {group.talents.map(t => {
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
                        {localizedName(t)}
                        {secondaryName(t) && <span className="muted small-text name-secondary"> · {secondaryName(t)}</span>}
                      </strong>
                      <div className="tag-row compact">
                        <span className="badge tier">Тир {t.tier}</span>
                        <span className="badge">{TALENT_CATEGORY_LABELS[t.category]}</span>
                        {t.isRanked && <span className="badge">Ранговый{ranksOwned > 0 ? `: ${ranksOwned}` : ''}</span>}
                        {t.isCustom && <span className="badge custom">Кастом</span>}
                        <span className="badge">{t.activation}</span>
                      </div>
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
                      {ownedRow && (
                        <button className="small" title="Печать карточки таланта"
                          onClick={() => setPrintTalent(ownedRow)}>
                          <Icon name="printer" className="button-icon" />
                        </button>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          ))}
        </div>
      </section>

      {pickFor && (
        <div className="modal-backdrop" onClick={() => setPickFor(null)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <h3>{localizedName(pickFor)}: выбор характеристики</h3>
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
