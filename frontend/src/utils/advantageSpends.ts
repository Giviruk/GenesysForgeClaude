import type { RuleTableEntry } from '../api/types'

export type AdvantageSpendContext = 'general' | 'combat' | 'social' | 'vehicle'

export interface AdvantageSpendOption {
  id: string
  cost: number
  /** Для переменной цены (например, Нокдаун) дополняет числовой минимум. */
  costLabelRu?: string
  costLabelEn?: string
  labelRu: string
  labelEn: string
  detailRu?: string
  detailEn?: string
  requiresSuccess?: boolean
  requiresFailure?: boolean
}

/** Числовая цена преимуществ из справочника или свойства; X-цена намеренно не угадывается. */
export function parseAdvantageCost(value: string): number | null {
  const match = /(\d+)\s*(?:Advantage|преимуществ)/i.exec(value)
  if (!match) return null
  const cost = Number(match[1])
  return Number.isFinite(cost) && cost > 0 ? cost : null
}

const GENERAL_CODES = new Set([
  'spend-combat_pos_001_recover_strain',
  'spend-combat_pos_002_boost_next_ally',
  'spend-combat_pos_003_notice_detail',
  'spend-combat_pos_008_boost_any_ally',
])

function groupFor(context: AdvantageSpendContext): string | null {
  if (context === 'combat') return 'combat'
  if (context === 'social') return 'social encounter'
  if (context === 'vehicle') return 'chase / vehicle encounter'
  return null
}

/** Оставляет только расходы, которые можно оплатить текущими нетто-преимуществами. */
export function affordableRuleSpends(
  entries: RuleTableEntry[],
  advantages: number,
  context: AdvantageSpendContext,
  successful = true,
): AdvantageSpendOption[] {
  const group = groupFor(context)
  return entries
    .filter(entry => entry.kind === 'symbolSpend')
    .filter(entry => context === 'general'
      ? GENERAL_CODES.has(entry.code)
      : (entry.groupEn ?? entry.groupRu).trim().toLowerCase() === group)
    .flatMap(entry => {
      const cost = parseAdvantageCost(entry.symbolCost)
      if (cost == null || cost > advantages) return []
      const requiresSuccess = entry.code === 'spend-combat_pos_011_incapacitate_instead_damage'
      if (requiresSuccess && !successful) return []
      return [{
        id: entry.code,
        cost,
        labelRu: entry.body,
        labelEn: entry.bodyEn || entry.body,
        requiresSuccess,
      }]
    })
}

export function affordableExtraSpends(
  options: AdvantageSpendOption[],
  advantages: number,
  successful = true,
): AdvantageSpendOption[] {
  return options.filter(option =>
    option.cost > 0 && option.cost <= advantages
    && (!option.requiresSuccess || successful)
    && (!option.requiresFailure || !successful))
}
