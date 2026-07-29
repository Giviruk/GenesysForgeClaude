import type { RuleTableEntry } from '../api/types'
import type { DieSymbol, RollSymbols } from './diceRoller'

export type AdvantageSpendContext = 'general' | 'combat' | 'social' | 'magic' | 'vehicle'

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
  /** Если задано, конкретную активацию также можно оплатить триумфами. */
  triumphCost?: number
}

export type OutcomeSpendKind = 'positive' | 'negative' | 'result'

export interface SymbolPayment {
  symbol: DieSymbol
  /** Минимальное число символов в результате, при котором вариант доступен. */
  threshold: number
  /** Число символов, которое тратится; null — эффект масштабируется числом успехов. */
  cost: number | null
  mode: 'fixed' | 'additional' | 'scaling'
}

export interface OutcomeSpendOption {
  id: string
  kind: OutcomeSpendKind
  payments: SymbolPayment[]
  labelRu: string
  labelEn: string
  detailRu?: string
  detailEn?: string
}

/** Числовая цена преимуществ из справочника или свойства; X-цена намеренно не угадывается. */
export function parseAdvantageCost(value: string): number | null {
  const match = /(\d+)\s*(?:Advantage|преимуществ)/i.exec(value)
  if (!match) return null
  const cost = Number(match[1])
  return Number.isFinite(cost) && cost > 0 ? cost : null
}

/**
 * Разбирает структурную стоимость строки справочника. Неизвестная X-стоимость не угадывается:
 * крит и качества добавляются роллером конкретного оружия с уже известной ценой.
 */
export function parseSymbolPayments(value: string): SymbolPayment[] {
  const payments: SymbolPayment[] = []
  const fixed: Array<[DieSymbol, string]> = [
    ['advantage', 'Advantage'],
    ['threat', 'Threat'],
    ['triumph', 'Triumph'],
    ['despair', 'Despair'],
    ['failure', 'Failure'],
  ]
  for (const [symbol, token] of fixed) {
    const match = new RegExp(`(\\d+)\\s*${token}`, 'i').exec(value)
    if (match) {
      const cost = Number(match[1])
      if (Number.isFinite(cost) && cost > 0) {
        payments.push({ symbol, threshold: cost, cost, mode: 'fixed' })
      }
    }
  }

  if (/Additional Success/i.test(value)) {
    payments.push({ symbol: 'success', threshold: 2, cost: 1, mode: 'additional' })
  } else if (/Successes/i.test(value)) {
    payments.push({ symbol: 'success', threshold: 1, cost: null, mode: 'scaling' })
  } else if (/\bSuccess\b/i.test(value)) {
    payments.push({ symbol: 'success', threshold: 1, cost: 1, mode: 'fixed' })
  }
  return payments
}

const GENERAL_CODES = new Set([
  'spend-combat_pos_001_recover_strain',
  'spend-combat_pos_002_boost_next_ally',
  'spend-combat_pos_003_notice_detail',
  'spend-combat_pos_008_boost_any_ally',
])

/** Социальные эквиваленты уже показанных общих трат не должны дублироваться. */
const SOCIAL_GENERAL_DUPLICATES = new Set([
  'spend-social_pos_001_recover_strain',
  'spend-social_pos_002_boost_next_ally',
  'spend-social_pos_003_notice_scene_detail',
  'spend-social_pos_006_boost_any_ally',
])

const CONTEXT_REQUIRED_CODES = new Set([
  'spend-combat_pos_004_inflict_critical',
  'spend-combat_pos_005_activate_item_quality',
])

function groupFor(context: AdvantageSpendContext): string | null {
  if (context === 'combat') return 'combat'
  if (context === 'social') return 'social encounter'
  if (context === 'vehicle') return 'chase / vehicle encounter'
  return null
}

function belongsToContext(entry: RuleTableEntry, context: AdvantageSpendContext): boolean {
  if (GENERAL_CODES.has(entry.code)) return true
  if (context === 'general' || context === 'magic') return false
  const group = groupFor(context)
  if ((entry.groupEn ?? entry.groupRu).trim().toLowerCase() !== group) return false
  return context !== 'social' || !SOCIAL_GENERAL_DUPLICATES.has(entry.code)
}

function canPay(payment: SymbolPayment, result: RollSymbols): boolean {
  return result[payment.symbol] >= payment.threshold
}

function spendKind(payments: SymbolPayment[]): OutcomeSpendKind {
  if (payments.some(p => p.symbol === 'threat' || p.symbol === 'despair')) return 'negative'
  if (payments.some(p => p.symbol === 'success' || p.symbol === 'failure')) return 'result'
  return 'positive'
}

/** Все доступные варианты справочника для полного результата броска. */
export function affordableOutcomeRuleSpends(
  entries: RuleTableEntry[],
  result: RollSymbols,
  context: AdvantageSpendContext,
): OutcomeSpendOption[] {
  return entries
    .filter(entry => entry.kind === 'symbolSpend')
    .filter(entry => belongsToContext(entry, context))
    // Эти строки корректны только при наличии конкретного оружия/качества. Их добавляет roller extra.
    .filter(entry => !CONTEXT_REQUIRED_CODES.has(entry.code))
    .flatMap(entry => {
      const payments = parseSymbolPayments(entry.symbolCost)
      if (payments.length === 0 || !payments.some(payment => canPay(payment, result))) return []
      if (entry.code === 'spend-combat_pos_011_incapacitate_instead_damage' && result.success <= 0) {
        return []
      }
      return [{
        id: entry.code,
        kind: spendKind(payments),
        payments,
        labelRu: entry.body,
        labelEn: entry.bodyEn || entry.body,
        detailRu: entry.notes || undefined,
        detailEn: entry.notesEn || entry.notes || undefined,
      }]
    })
}

/** Контекстные активации оружия/магии, стоимость которых известна только самому броску. */
export function affordableExtraOutcomeSpends(
  options: AdvantageSpendOption[],
  result: RollSymbols,
): OutcomeSpendOption[] {
  const successful = result.success > 0
  return options
    .filter(option =>
      option.cost > 0
      && (!option.requiresSuccess || successful)
      && (!option.requiresFailure || !successful))
    .flatMap(option => {
      const payments: SymbolPayment[] = [{
        symbol: 'advantage', threshold: option.cost, cost: option.cost, mode: 'fixed',
      }]
      if (option.triumphCost && option.triumphCost > 0) {
        payments.push({
          symbol: 'triumph',
          threshold: option.triumphCost,
          cost: option.triumphCost,
          mode: 'fixed',
        })
      }
      if (!payments.some(payment => canPay(payment, result))) return []
      return [{
        id: option.id,
        kind: 'positive' as const,
        payments,
        labelRu: option.labelRu,
        labelEn: option.labelEn,
        detailRu: [
          option.costLabelRu,
          option.detailRu,
        ].filter(Boolean).join(' · ') || undefined,
        detailEn: [
          option.costLabelEn,
          option.detailEn,
        ].filter(Boolean).join(' · ') || undefined,
      }]
    })
}

/** Оставляет только расходы, которые можно оплатить текущими нетто-преимуществами. */
export function affordableRuleSpends(
  entries: RuleTableEntry[],
  advantages: number,
  context: AdvantageSpendContext,
  successful = true,
): AdvantageSpendOption[] {
  return entries
    .filter(entry => entry.kind === 'symbolSpend')
    .filter(entry => belongsToContext(entry, context))
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
