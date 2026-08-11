import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterSheet, Reference, TalentCategory, TalentDef } from '../api/types'
import { TalentsTab } from './TalentsTab'

const buyTalentMock = vi.fn()
const npcsMock = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    buyTalent: (...args: unknown[]) => buyTalentMock(...args),
    npcs: (...args: unknown[]) => npcsMock(...args),
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
  activationEn: '',
  canUseOutOfTurn: false,
  careerSkillNames: [],
  linkCode: '',
  requiresTalentCode: '',
  excludesTalentCodes: [],
  usesPerScope: 0,
  useScope: 'none',
  storyPointCost: 0,
  strainCost: 0,
  trigger: '',
  choiceKind: 'none',
  choiceCountFirstRank: 0,
  choiceCountNextRank: 0,
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
  system: 'genesysCore',
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
    buyTalentMock.mockResolvedValue(undefined)
    npcsMock.mockResolvedValue([{
      id: 'npc-falcon', name: 'Серый сокол', system: 'genesysCore', silhouette: 0,
    }])
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

  it('предлагает выбрать животное-спутника и передаёт выбор при покупке', async () => {
    const companion = {
      ...talent('animal-companion', 'Животное-спутник', 'general'),
      tier: 3,
      isRanked: true,
      choiceKind: 'animalCompanion' as const,
      choiceCountFirstRank: 1,
      choiceCountNextRank: 1,
    }
    const companionSheet = {
      ...sheet,
      talentTierCounts: { '1': 3, '2': 2 },
    } as unknown as CharacterSheet
    const refresh = vi.fn().mockResolvedValue(undefined)

    render(<TalentsTab sheet={companionSheet}
      reference={{ talents: [companion] } as unknown as Reference}
      onError={() => {}} refresh={refresh} />)

    fireEvent.click(screen.getByRole('button', { name: /Купить/ }))
    expect(buyTalentMock).not.toHaveBeenCalled()
    expect(screen.getByRole('heading', { name: /выбор спутника/ })).toBeTruthy()
    await screen.findByRole('option', { name: /Серый сокол/ })
    fireEvent.click(screen.getByRole('button', { name: 'Подтвердить и купить' }))

    await waitFor(() => expect(buyTalentMock).toHaveBeenCalledWith(
      'char-1', 'animal-companion', undefined, ['npc-falcon']))
    expect(refresh).toHaveBeenCalledOnce()
  })

  it('показывает сохранённого спутника у купленного таланта', () => {
    const companion = {
      ...talent('animal-companion', 'Животное-спутник', 'general'),
      tier: 3, isRanked: true, choiceKind: 'animalCompanion' as const,
      choiceCountFirstRank: 1, choiceCountNextRank: 1,
    }
    const companionSheet = {
      ...sheet,
      talents: [{
        talentDefId: companion.id, name: companion.name, nameRu: companion.nameRu,
        tier: 3, isRanked: true, ranks: 1, activation: 'Пассивный', description: '',
        woundBonus: 0, strainBonus: 0, soakBonus: 0, meleeDefenseBonus: 0,
        rangedDefenseBonus: 0, grantsCharacteristic: false, grantedCharacteristics: [],
        choices: [{ rankIndex: 0, kind: 'animalCompanion', value: 'npc-falcon',
          displayName: 'Серый сокол' }], needsChoice: false, activationEn: 'Passive',
        canUseOutOfTurn: false,
      }],
      talentTierCounts: { '1': 3, '2': 2, '3': 1 },
    } as unknown as CharacterSheet

    render(<TalentsTab sheet={companionSheet}
      reference={{ talents: [companion] } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    expect(screen.getAllByText(/Выбор:.*Серый сокол/).length).toBeGreaterThan(0)
  })
})
