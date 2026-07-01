import { describe, expect, it } from 'vitest'
import { talentBonusSummary, type TalentBonuses } from './talentBonuses'

const none: TalentBonuses = {
  woundBonus: 0, strainBonus: 0, soakBonus: 0, meleeDefenseBonus: 0, rangedDefenseBonus: 0,
}

describe('talentBonusSummary', () => {
  it('пустой список для таланта без пассивных бонусов', () => {
    expect(talentBonusSummary(none, 1)).toEqual([])
  })

  it('один ранг — без множителя', () => {
    expect(talentBonusSummary({ ...none, strainBonus: 1 }, 1)).toEqual(['+1 к порогу усталости'])
  })

  it('несколько рангов — сумма и расшифровка', () => {
    expect(talentBonusSummary({ ...none, woundBonus: 2 }, 2)).toEqual(['+4 к порогу ран (2 ранга × +2)'])
  })

  it('несколько бонусов сразу (Defensive: обе защиты)', () => {
    expect(talentBonusSummary({ ...none, meleeDefenseBonus: 1, rangedDefenseBonus: 1 }, 1)).toEqual([
      '+1 к защите (ближней)',
      '+1 к защите (дальней)',
    ])
  })

  it('отрицательный бонус отображается со знаком минус', () => {
    expect(talentBonusSummary({ ...none, soakBonus: -1 }, 2)).toEqual(['-2 к поглощению (2 ранга × -1)'])
  })
})
