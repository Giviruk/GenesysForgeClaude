import { describe, expect, it } from 'vitest'
import type { RuleTableEntry } from '../api/types'
import {
  affordableExtraSpends, affordableRuleSpends, parseAdvantageCost,
} from './advantageSpends'

const rule = (over: Partial<RuleTableEntry>): RuleTableEntry => ({
  id: 'rule', kind: 'symbolSpend', code: 'rule', nameRu: '', nameEn: '',
  groupRu: 'Бой', groupEn: 'Combat', sortOrder: 1, rollRange: '',
  symbolCost: '1 Advantage или 1 Triumph', body: 'Трата', bodyEn: 'Spend',
  notes: '', source: 'Test', sourcePage: '', ...over,
})

describe('advantage spends', () => {
  it('разбирает только определённую числовую цену преимуществ', () => {
    expect(parseAdvantageCost('2 Advantage или 1 Triumph')).toBe(2)
    expect(parseAdvantageCost('1 преимущество')).toBe(1)
    expect(parseAdvantageCost('X Advantage или 1 Triumph')).toBeNull()
    expect(parseAdvantageCost('1 Triumph')).toBeNull()
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
})
