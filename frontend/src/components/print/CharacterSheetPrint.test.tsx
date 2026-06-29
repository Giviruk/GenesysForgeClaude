import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { CharacterSheet, Reference } from '../../api/types'
import { CharacterSheetPrint } from './CharacterSheetPrint'

vi.mock('../../api/client', () => ({
  api: { notes: vi.fn().mockResolvedValue([]) },
}))

const sheet = {
  id: 'character-1',
  name: 'Эйрин',
  system: 'realmsOfTerrinoth',
  archetype: { name: 'Elf' },
  career: { name: 'Mage' },
  characteristics: {
    brawn: 2, agility: 3, intellect: 4, cunning: 3, willpower: 3, presence: 2,
  },
  totalXp: 175,
  spentXp: 113,
  availableXp: 62,
  isCreationPhase: false,
  woundsCurrent: 12,
  strainCurrent: 6,
  money: 10,
  derived: {
    woundThreshold: 14,
    strainThreshold: 13,
    soak: 4,
    meleeDefense: 1,
    rangedDefense: 2,
    encumbranceThreshold: 9,
    encumbranceLoad: 5,
    encumbered: false,
  },
  skills: [
    {
      skillDefId: 'skill-athletics',
      name: 'Athletics',
      kind: 'general',
      characteristic: 'brawn',
      ranks: 1,
      isCareer: true,
      pool: { ability: 1, proficiency: 1 },
      nextRankCost: 10,
      freeRanks: 0,
    },
    {
      skillDefId: 'skill-melee',
      name: 'Melee',
      kind: 'combat',
      characteristic: 'brawn',
      ranks: 2,
      isCareer: true,
      pool: { ability: 0, proficiency: 2 },
      nextRankCost: 15,
      freeRanks: 0,
    },
  ],
  talents: [],
  talentTierCounts: {},
  heroicAbility: null,
  heroicUpgradeRank: 0,
  heroicUpgradePointsTotal: 0,
  heroicUpgradePointsSpent: 0,
  items: [{
    id: 'item-1',
    itemDefId: 'weapon-1',
    name: 'Sword',
    kind: 'weapon',
    state: 'equipped',
    quantity: 1,
    encumbrance: 1,
    soakBonus: 0,
    meleeDefense: 0,
    rangedDefense: 0,
    encumbranceThresholdBonus: 0,
    load: 1,
    description: '',
    price: 100,
    skillName: 'Melee (Light)',
    damage: '+2',
    crit: '3',
    rangeBand: 'Engaged',
    properties: '',
  }],
  desire: 'Защитить деревню',
  fear: null,
  strength: null,
  flaw: 'Вспыльчивость',
  background: 'Родилась в лесу.',
  criticalInjuries: [
    { id: 'ci-1', ruleCode: 'crit-ci_001_005', nameRu: 'Небольшая царапина', severity: 'Лёгкая', rollResult: 3, notes: null },
  ],
} as unknown as CharacterSheet

const reference = {
  skills: [
    { id: 'skill-athletics', name: 'Athletics', nameRu: 'Атлетика' },
    { id: 'skill-melee', name: 'Melee', nameRu: 'Ближний бой' },
  ],
  items: [{ id: 'weapon-1', name: 'Sword', nameRu: 'Меч' }],
} as unknown as Reference

describe('CharacterSheetPrint', () => {
  it('показывает compact stat-блок и dice pool оружия', async () => {
    const { container } = render(<CharacterSheetPrint sheet={sheet} reference={reference} />)

    expect(screen.getByText('12 / 14')).toBeTruthy()
    expect(screen.getByText('5 / 9')).toBeTruthy()
    expect(screen.getByText('Меч (Sword)')).toBeTruthy()
    expect(container.querySelector('.sheet-weapon-pool .dice-pool')
      ?.getAttribute('title')).toBe('2 мастерства + 0 способности')
    expect(container.querySelectorAll('.sheet-stat')).toHaveLength(12)
    expect(screen.getByRole('heading', { name: 'Общие' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Боевые' })).toBeTruthy()
    expect(container.querySelectorAll('.sheet-skill-column')).toHaveLength(2)
    await waitFor(() => expect(screen.queryByText('Заметки')).toBeNull())
  })

  it('печатает критические ранения (U-23)', () => {
    render(<CharacterSheetPrint sheet={sheet} reference={reference} />)
    expect(screen.getByRole('heading', { name: 'Критические ранения' })).toBeTruthy()
    expect(screen.getByText('Небольшая царапина')).toBeTruthy()
  })

  it('печатает мотивации и предысторию (U-22), пропуская пустые', () => {
    render(<CharacterSheetPrint sheet={sheet} reference={reference} />)
    expect(screen.getByRole('heading', { name: 'Образ персонажа' })).toBeTruthy()
    expect(screen.getByText('Защитить деревню')).toBeTruthy()
    expect(screen.getByText('Вспыльчивость')).toBeTruthy()
    expect(screen.getByText('Родилась в лесу.')).toBeTruthy()
    expect(screen.queryByText('Страх:')).toBeNull() // пустое поле не выводится
  })
})
