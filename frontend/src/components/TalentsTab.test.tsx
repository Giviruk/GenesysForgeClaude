import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterSheet, Reference, TalentCategory, TalentDef } from '../api/types'
import { TalentsTab } from './TalentsTab'

vi.mock('../api/client', () => ({
  api: {
    buyTalent: vi.fn(),
    refundTalent: vi.fn(),
  },
}))

const talent = (id: string, nameRu: string, category: TalentCategory): TalentDef => ({
  id,
  name: nameRu,
  nameRu,
  tier: 1,
  isRanked: false,
  category,
  setting: 'any',
  activation: 'Пассивный',
  description: `${nameRu}: описание`,
  safeDescription: `${nameRu}: описание`,
  source: 'Test',
  woundBonus: 0,
  strainBonus: 0,
  soakBonus: 0,
  meleeDefenseBonus: 0,
  rangedDefenseBonus: 0,
  isCustom: false,
  grantsCharacteristic: false,
})

const sheet = {
  id: 'char-1',
  availableXp: 100,
  isCreationPhase: true,
  talents: [],
  talentTierCounts: {},
  characteristics: { brawn: 2, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2 },
} as unknown as CharacterSheet

const reference = {
  talents: [
    talent('combat-1', 'Боевой талант', 'combat'),
    talent('social-1', 'Социальный талант', 'social'),
    talent('magic-1', 'Магический талант', 'magic'),
  ],
} as unknown as Reference

describe('TalentsTab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('фильтрует доступные таланты по категории', () => {
    render(<TalentsTab sheet={sheet} reference={reference} onError={() => {}} refresh={() => Promise.resolve()} />)

    expect(screen.getByText('Боевой талант')).toBeTruthy()
    expect(screen.getByText('Социальный талант')).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: /Социальные/ }))

    expect(screen.queryByText('Боевой талант')).toBeNull()
    expect(screen.getByText('Социальный талант')).toBeTruthy()
    expect(screen.queryByText('Магический талант')).toBeNull()
  })
})
