import { useState } from 'react'
import { api } from '../api/client'
import type { Characteristic, CharacterSheet, Reference, SheetTalent, TalentCategory, TalentDef } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, localizedName, nextRankTier, secondaryName,
  TALENT_CATEGORIES, TALENT_CATEGORY_LABELS, talentCost,
} from '../utils/labels'
import { canPurchaseTier, canRemoveTier } from '../utils/pyramid'
import { lang, t } from '../i18n'
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
// Активация хранится строкой данных (обычно по-русски) — проверяем оба языка.
const isPassiveActivation = (activation: string) => {
  const a = activation.toLowerCase()
  return a.startsWith('пассив') || a.startsWith('passive')
}
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
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
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
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
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
      return localizedName(a).localeCompare(localizedName(b), lang)
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
      label: `${TALENT_CATEGORY_LABELS[talent.category]} · ${t('Тир', 'Tier')} ${talent.tier}`,
      talents: [talent],
    })
    return groups
  }, [])

  if (printTalent) {
    return (
      <PrintPreview title={t(`Талант — ${printTalent.name}`, `Talent — ${printTalent.name}`)} onClose={() => setPrintTalent(null)}>
        {() => <TalentCard talent={printTalent} />}
      </PrintPreview>
    )
  }

  return (
    <div>
      <section className="panel">
        <div className="section-title-line">
          <h3>{t('Пирамида талантов', 'Talent pyramid')}</h3>
        </div>
        <p className="hint">
          {t(
            'Талантов каждого тира должно быть строго больше, чем талантов тира выше. ' +
            'Стоимость — тир × 5 XP. Ранговые таланты: каждый следующий ранг покупается на тир выше.',
            'Each tier must contain strictly more talents than the tier above it. ' +
            'Cost is tier × 5 XP. Ranked talents: each further rank is bought one tier higher.',
          )}
        </p>
        {sheet.talents.length === 0 && (
          <p className="muted">{t('Пока нет талантов — купите первый из списка ниже.', 'No talents yet — buy the first one from the list below.')}</p>
        )}
        <div className="pyramid">
          {TIERS.map(tier => {
            const slots = sheet.talentTierCounts[String(tier)] ?? 0
            const cards = cardsByTier.get(tier)!
            return (
              <div key={tier} className="pyramid-tier">
                <div className="tier-label">{t('Тир', 'Tier')} {tier} <span className="muted">({slots})</span></div>
                <div className="tier-cells">
                  {cards.map(c => {
                    const isPassive = isPassiveActivation(c.activation)
                    return (
                      <div key={c.key} className="pyramid-card">
                        <div className="owned-talent-head">
                          <strong>{c.name}</strong>
                          {c.rankNo && <span className="badge">{t('ранг', 'rank')} {c.rankNo}</span>}
                          <span className={isPassive ? 'badge passive' : 'badge active'}>
                            {isPassive ? t('Пассивный', 'Passive') : `${t('Активный', 'Active')} · ${c.activation}`}
                          </span>
                        </div>
                        <p className="owned-talent-desc">{c.description}</p>
                        {c.grant && (
                          <div className="bonus-line" title={t('Характеристика увеличена этим талантом', 'Characteristic increased by this talent')}>
                            ⬆ +1 {CHARACTERISTIC_LABELS[c.grant]}
                          </div>
                        )}
                        {c.bonuses.length > 0 && (
                          <div className="bonus-line" title={t('Применяется к производным характеристикам автоматически', 'Applied to derived characteristics automatically')}>
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
          <h3>{t('Купленные таланты', 'Purchased talents')}</h3>
        </div>
        {sheet.talents.length === 0 ? (
          <p className="muted">{t('Купленных талантов пока нет.', 'No purchased talents yet.')}</p>
        ) : (
          <div className="owned-talents">
            {sheet.talents
              .toSorted((a, b) => a.tier - b.tier || localizedName(a).localeCompare(localizedName(b), lang))
              .map(tal => {
                const isPassive = isPassiveActivation(tal.activation)
                const bonuses = talentBonusSummary(tal, tal.ranks)
                return (
                  <div key={tal.talentDefId} className="owned-talent">
                    <div className="owned-talent-head">
                      <strong>{localizedName(tal)}</strong>
                      <span className="badge tier">{t('Тир', 'Tier')} {tal.tier}</span>
                      {tal.isRanked && <span className="badge">{t('Ранги:', 'Ranks:')} {tal.ranks}</span>}
                      <span className={isPassive ? 'badge passive' : 'badge active'}>
                        {isPassive ? t('Пассивный', 'Passive') : tal.activation}
                      </span>
                    </div>
                    <p className="owned-talent-desc muted">{tal.description}</p>
                    {bonuses.length > 0 && <div className="bonus-line">{bonuses.join(' · ')}</div>}
                  </div>
                )
              })}
          </div>
        )}
      </section>

      <section className="panel">
        <div className="section-title-line">
          <h3>{t('Доступные таланты', 'Available talents')}</h3>
          <span className="section-count">{visibleTalents.length} {t('из', 'of')} {reference.talents.length}</span>
        </div>
        <div className="tabs">
          <button className={categoryFilter === 'all' ? 'tab active' : 'tab'} onClick={() => setCategoryFilter('all')}>
            {t('Все', 'All')} <span className="muted">({reference.talents.length})</span>
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
            {t('Все тиры', 'All tiers')} <span className="muted">({reference.talents.filter(matchesCategory).length})</span>
          </button>
          {TIERS.map(tier => {
            const count = reference.talents.filter(t => t.tier === tier && matchesCategory(t)).length
            return (
              <button key={tier}
                className={activeTier === tier ? 'tab active' : 'tab'}
                onClick={() => setActiveTier(tier)}>
                {t('Тир', 'Tier')} {tier} <span className="muted">({count})</span>
              </button>
            )
          })}
        </div>
        <div className="talent-list grouped">
          {visibleTalents.length === 0 && (
            <p className="muted">{t('Нет талантов в выбранной категории и тире.', 'No talents in the chosen category and tier.')}</p>
          )}
          {talentGroups.map(group => (
            <div key={group.key} className="talent-group">
              <div className="talent-group-title">{group.label}</div>
              {group.talents.map(tal => {
                const ownedRow = owned.get(tal.id)
                const ranksOwned = ownedRow?.ranks ?? 0
                const maxedOut = ownedRow && !tal.isRanked
                const effectiveTier = nextRankTier(tal.tier, ranksOwned)
                const cost = talentCost(effectiveTier)
                const pyramidOk = canPurchaseTier(sheet.talentTierCounts, effectiveTier)
                const affordable = cost <= sheet.availableXp
                const noGrantsLeft = tal.grantsCharacteristic && grantableCharacteristics(tal).length === 0
                const reason = maxedOut ? t('Уже куплен', 'Already purchased')
                  : !pyramidOk ? t('Нарушит пирамиду', 'Would break the pyramid')
                  : !affordable ? t('Недостаточно XP', 'Not enough XP')
                  : noGrantsLeft ? t('Нет характеристик для увеличения', 'No characteristics left to increase')
                  : null
                return (
                  <div key={tal.id} className="talent-row">
                    <div className="talent-info">
                      <strong>
                        {localizedName(tal)}
                        {secondaryName(tal) && <span className="muted small-text name-secondary"> · {secondaryName(tal)}</span>}
                      </strong>
                      <div className="tag-row compact">
                        <span className="badge tier">{t('Тир', 'Tier')} {tal.tier}</span>
                        <span className="badge">{TALENT_CATEGORY_LABELS[tal.category]}</span>
                        {tal.isRanked && <span className="badge">{t('Ранговый', 'Ranked')}{ranksOwned > 0 ? `: ${ranksOwned}` : ''}</span>}
                        {tal.isCustom && <span className="badge custom">{t('Кастом', 'Custom')}</span>}
                        <span className="badge">{tal.activation}</span>
                      </div>
                      <p className="muted">{tal.description}</p>
                    </div>
                    <div className="talent-actions">
                      {sheet.isCreationPhase && ranksOwned > 0 && (() => {
                        const lastTier = nextRankTier(tal.tier, ranksOwned - 1)
                        const removable = canRemoveTier(sheet.talentTierCounts, lastTier)
                        return (
                          <button className="small" disabled={!removable}
                            title={removable
                              ? t(`Вернуть ${tal.isRanked ? `ранг ${ranksOwned}` : 'талант'} (+${talentCost(lastTier)} XP)`,
                                  `Refund ${tal.isRanked ? `rank ${ranksOwned}` : 'the talent'} (+${talentCost(lastTier)} XP)`)
                              : t('Нельзя вернуть: нарушится пирамида', 'Cannot refund: it would break the pyramid')}
                            onClick={() => refund(tal)}>
                            {t('Вернуть', 'Refund')}
                          </button>
                        )
                      })()}
                      <button className="small" disabled={!!reason} title={reason ?? ''}
                        onClick={() => (tal.grantsCharacteristic ? setPickFor(tal) : buy(tal))}>
                        {maxedOut ? t('Куплен', 'Purchased') : t(`Купить (${cost} XP${tal.isRanked && ranksOwned > 0 ? `, тир ${effectiveTier}` : ''})`, `Buy (${cost} XP${tal.isRanked && ranksOwned > 0 ? `, tier ${effectiveTier}` : ''})`)}
                      </button>
                      {ownedRow && (
                        <button className="small" title={t('Печать карточки таланта', 'Print the talent card')}
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
            <h3>{localizedName(pickFor)}: {t('выбор характеристики', 'choose a characteristic')}</h3>
            <p className="hint">{t(`Талант увеличивает выбранную характеристику на 1 (не выше ${TALENT_CHARACTERISTIC_MAX}).`, `The talent increases the chosen characteristic by 1 (up to ${TALENT_CHARACTERISTIC_MAX}).`)}</p>
            <div className="chips">
              {grantableCharacteristics(pickFor).map(c => (
                <button key={c} type="button" className="chip"
                  onClick={() => confirmPick(c)}>
                  {CHARACTERISTIC_LABELS[c]} <span className="muted">{sheet.characteristics[c]} → {sheet.characteristics[c] + 1}</span>
                </button>
              ))}
            </div>
            <div className="modal-actions">
              <button type="button" onClick={() => setPickFor(null)}>{t('Отмена', 'Cancel')}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
