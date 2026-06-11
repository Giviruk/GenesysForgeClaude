import { describe, expect, it } from 'vitest'
import { canPurchaseTier } from './pyramid'
import { nextRankTier, talentCost } from './labels'

describe('canPurchaseTier (правило пирамиды)', () => {
  it('первый талант тира 1 доступен', () => {
    expect(canPurchaseTier({}, 1)).toBe(true)
  })

  it('тир 2 без талантов тира 1 недоступен', () => {
    expect(canPurchaseTier({}, 2)).toBe(false)
  })

  it('тир 2 при одном таланте тира 1 недоступен', () => {
    expect(canPurchaseTier({ '1': 1 }, 2)).toBe(false)
  })

  it('тир 2 при двух талантах тира 1 доступен', () => {
    expect(canPurchaseTier({ '1': 2 }, 2)).toBe(true)
  })

  it('второй талант тира 2 требует трёх талантов тира 1', () => {
    expect(canPurchaseTier({ '1': 2, '2': 1 }, 2)).toBe(false)
    expect(canPurchaseTier({ '1': 3, '2': 1 }, 2)).toBe(true)
  })

  it('тир 5 требует полной пирамиды', () => {
    expect(canPurchaseTier({ '1': 5, '2': 4, '3': 3, '4': 2 }, 5)).toBe(true)
    expect(canPurchaseTier({ '1': 5, '2': 4, '3': 2, '4': 2 }, 5)).toBe(false)
  })

  it('некорректный тир отклоняется', () => {
    expect(canPurchaseTier({}, 0)).toBe(false)
    expect(canPurchaseTier({}, 6)).toBe(false)
  })
})

describe('стоимость и тиры талантов', () => {
  it('стоимость = тир × 5', () => {
    expect(talentCost(1)).toBe(5)
    expect(talentCost(5)).toBe(25)
  })

  it('ранговый талант дорожает на тир за ранг, максимум 5', () => {
    expect(nextRankTier(1, 0)).toBe(1)
    expect(nextRankTier(1, 1)).toBe(2)
    expect(nextRankTier(4, 3)).toBe(5)
  })
})
