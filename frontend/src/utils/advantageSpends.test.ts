import { describe, expect, it } from 'vitest'
import type { RuleTableEntry } from '../api/types'
import {
  affordableExtraOutcomeSpends,
  affordableExtraSpends,
  affordableOutcomeRuleSpends,
  affordableRuleSpends,
  parseAdvantageCost,
  parseSymbolPayments,
} from './advantageSpends'
import type { RollSymbols } from './diceRoller'

const rule = (over: Partial<RuleTableEntry>): RuleTableEntry => ({
  id: 'rule', kind: 'symbolSpend', code: 'rule', nameRu: '', nameEn: '',
  groupRu: 'Бой', groupEn: 'Combat', sortOrder: 1, rollRange: '',
  symbolCost: '1 Advantage или 1 Triumph', body: 'Трата', bodyEn: 'Spend',
  notes: '', source: 'Test', sourcePage: '', ...over,
})

const result = (over: Partial<RollSymbols>): RollSymbols => ({
  success: 0, failure: 0, advantage: 0, threat: 0, triumph: 0, despair: 0, ...over,
})

describe('advantage spends', () => {
  it('разбирает только определённую числовую цену преимуществ', () => {
    expect(parseAdvantageCost('2 Advantage или 1 Triumph')).toBe(2)
    expect(parseAdvantageCost('1 преимущество')).toBe(1)
    expect(parseAdvantageCost('X Advantage или 1 Triumph')).toBeNull()
    expect(parseAdvantageCost('1 Triumph')).toBeNull()
  })

  it('разбирает альтернативы и масштабируемые успехи без догадки об X-цене', () => {
    expect(parseSymbolPayments('2 Threat или 1 Despair')).toEqual([
      { symbol: 'threat', threshold: 2, cost: 2, mode: 'fixed' },
      { symbol: 'despair', threshold: 1, cost: 1, mode: 'fixed' },
    ])
    expect(parseSymbolPayments('X Advantage или 1 Triumph')).toEqual([
      { symbol: 'triumph', threshold: 1, cost: 1, mode: 'fixed' },
    ])
    expect(parseSymbolPayments('Additional Success')).toEqual([
      { symbol: 'success', threshold: 2, cost: 1, mode: 'additional' },
    ])
    expect(parseSymbolPayments('Successes')).toEqual([
      { symbol: 'success', threshold: 1, cost: null, mode: 'scaling' },
    ])
  })

  it('оставляет доступные расходы выбранного контекста', () => {
    const entries = [
      rule({ code: 'combat-1', symbolCost: '1 Advantage' }),
      rule({ code: 'combat-3', symbolCost: '3 Advantage' }),
      rule({ code: 'social-1', groupRu: 'Социальная сцена', groupEn: 'Social encounter' }),
    ]
    expect(affordableRuleSpends(entries, 2, 'combat').map(x => x.id)).toEqual(['combat-1'])
  })

  it('не предлагает требующую попадания активацию на провале', () => {
    const option = {
      id: 'critical', cost: 2, labelRu: 'Крит', labelEn: 'Critical', requiresSuccess: true,
    }
    expect(affordableExtraSpends([option], 2, false)).toEqual([])
    expect(affordableExtraSpends([option], 2, true)).toEqual([option])
  })

  it('добавляет общие варианты всем контекстам, а специальные — только совпадающему', () => {
    const entries = [
      rule({ code: 'spend-combat_pos_001_recover_strain', groupEn: 'Combat', symbolCost: '1 Advantage' }),
      rule({ code: 'combat-only', groupEn: 'Combat', symbolCost: '1 Advantage' }),
      rule({ code: 'social-only', groupEn: 'Social encounter', symbolCost: '1 Advantage' }),
    ]
    const rolled = result({ advantage: 1 })
    expect(affordableOutcomeRuleSpends(entries, rolled, 'general').map(x => x.id))
      .toEqual(['spend-combat_pos_001_recover_strain'])
    expect(affordableOutcomeRuleSpends(entries, rolled, 'magic').map(x => x.id))
      .toEqual(['spend-combat_pos_001_recover_strain'])
    expect(affordableOutcomeRuleSpends(entries, rolled, 'combat').map(x => x.id))
      .toEqual(['spend-combat_pos_001_recover_strain', 'combat-only'])
    expect(affordableOutcomeRuleSpends(entries, rolled, 'social').map(x => x.id))
      .toEqual(['spend-combat_pos_001_recover_strain', 'social-only'])
  })

  it('фильтрует угрозы, триумфы и крахи по фактически выпавшему результату', () => {
    const entries = [
      rule({ code: 'triumph', symbolCost: '1 Triumph' }),
      rule({ code: 'threat', symbolCost: '2 Threat или 1 Despair' }),
      rule({ code: 'too-many-despairs', symbolCost: '2 Despair' }),
    ]
    const options = affordableOutcomeRuleSpends(
      entries,
      result({ triumph: 1, threat: 2, despair: 1 }),
      'combat',
    )
    expect(options.map(x => [x.id, x.kind])).toEqual([
      ['triumph', 'positive'],
      ['threat', 'negative'],
    ])
  })

  it('позволяет триумфу оплатить только явно разрешённую конкретную активацию', () => {
    const options = [
      { id: 'critical', cost: 3, triumphCost: 1, labelRu: 'Крит', labelEn: 'Critical' },
      { id: 'magic-extra', cost: 1, labelRu: 'Цель', labelEn: 'Target' },
    ]
    expect(affordableExtraOutcomeSpends(options, result({ triumph: 1 })).map(x => x.id))
      .toEqual(['critical'])
  })
})
