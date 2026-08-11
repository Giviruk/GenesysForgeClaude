import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { GameSession, NpcDetail, Reference } from '../api/types'
import type { DiceRollerRequest } from '../dice-roller-store'
import { GameTableTab } from './GameTableTab'

const sessionMock = vi.fn()
const npcMock = vi.fn()
const referenceMock = vi.fn()
const rollsMock = vi.fn()
const createRollMock = vi.fn()
const openRollerMock = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    session: (...args: unknown[]) => sessionMock(...args),
    npc: (...args: unknown[]) => npcMock(...args),
    reference: (...args: unknown[]) => referenceMock(...args),
    rolls: (...args: unknown[]) => rollsMock(...args),
    createRoll: (...args: unknown[]) => createRollMock(...args),
    rules: vi.fn().mockResolvedValue({ entries: [] }),
  },
}))

vi.mock('../dice-roller-store', async importOriginal => {
  const actual = await importOriginal<typeof import('../dice-roller-store')>()
  return { ...actual, useDiceRoller: () => ({ openRoller: openRollerMock, closeRoller: vi.fn() }) }
})

const session: GameSession = {
  id: 'session-1', campaignId: 'campaign-1', name: 'Засада', description: '', isActive: true,
  isGm: true, allowPlayerEdits: false, playerStoryPoints: 1, gmStoryPoints: 1,
  currentRound: 1, currentTurnIndex: 0, publicNotes: '', gmNotes: '',
  participants: [{
    id: 'participant-1', characterId: null, npcId: 'npc-1', displayName: 'Гоблины',
    participantType: 'minionGroup', initiativeSlotType: 'npc', count: 3,
    woundsCurrent: 1, woundsThreshold: 15, strainCurrent: 0, strainThreshold: null,
    soak: 3, meleeDefense: 0, rangedDefense: 0, criticalInjuries: 0,
    isActive: true, isDefeated: false, isHiddenFromPlayers: false, notes: '', order: 0,
  }],
  slots: [],
}

const npc: NpcDetail = {
  id: 'npc-1', name: 'Гоблин', system: 'realmsOfTerrinoth', kind: 'minion', role: 'skirmisher',
  description: 'Небольшой опасный противник.', source: 'Test', brawn: 3, agility: 2,
  intellect: 1, cunning: 2, willpower: 1, presence: 1, woundThreshold: 5,
  strainThreshold: null, soak: 3, meleeDefense: 0, rangedDefense: 0, silhouette: 1,
  tactics: 'Держатся группой.', visibility: 'private', campaignId: 'campaign-1',
  isMine: true, isBuiltIn: false, skills: [{ name: 'Melee', ranks: 0 }],
  abilities: [{ name: 'Стая', description: 'Действуют сообща.' }],
  attacks: [{ name: 'Клинок', skillName: 'Melee', damage: '+1', critical: '3', rangeBand: 'Engaged',
    notes: '', qualities: [], sourceWeapon: '' }],
  talents: ['Быстрый'], equipment: ['Клинок'], tags: [], warnings: [], createdAt: '', updatedAt: '',
}

const reference = {
  skills: [{ id: 'melee', name: 'Melee', nameRu: 'Ближний бой', characteristic: 'brawn',
    kind: 'combat', safeDescription: '', source: '', isCustom: false }],
  items: [], heroicAbilities: [], qualities: [], archetypes: [], careers: [], talents: [],
  heroicSecondaryEffects: [], attachments: [], mounts: [],
} as Reference

describe('GameTableTab — статблок и броски NPC', () => {
  beforeEach(() => {
    sessionMock.mockReset().mockResolvedValue(session)
    npcMock.mockReset().mockResolvedValue(npc)
    referenceMock.mockReset().mockResolvedValue(reference)
    rollsMock.mockReset().mockResolvedValue([])
    createRollMock.mockReset().mockResolvedValue({})
    openRollerMock.mockReset()
  })

  it('открывает статблок кликом по карточке и учитывает размер группы в навыках', async () => {
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Открыть статблок NPC Гоблины' }))
    const dialog = await screen.findByRole('dialog', { name: 'Статблок NPC: Гоблины' })

    expect(npcMock).toHaveBeenCalledWith('npc-1')
    expect(await within(dialog).findByText('Группа: эффективный ранг групповых навыков 2')).toBeTruthy()
    expect(within(dialog).getByText('Небольшой опасный противник.')).toBeTruthy()
    expect(within(dialog).getByText('Держатся группой.')).toBeTruthy()

    fireEvent.click(within(dialog).getByRole('button', { name: '🎲 Бросить' }))
    const request = openRollerMock.mock.calls[0][0] as Extract<DiceRollerRequest, { kind: 'roll' }>
    expect(request.initialPool).toEqual({ ability: 1, proficiency: 2 })
    expect(request.label).toBe('Melee')

    request.onLog?.({ poolJson: '{}', resultJson: '{}', summary: 'ok', label: 'Melee', isSecret: false })
    await waitFor(() => expect(createRollMock).toHaveBeenCalledWith('campaign-1', expect.objectContaining({
      actorName: 'Гоблины', label: 'Melee',
    })))
  })

  it('открывает боевой дайсроллер атаки с тем же групповым пулом', async () => {
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)
    fireEvent.click(await screen.findByRole('button', { name: 'Открыть статблок NPC Гоблины' }))
    const dialog = await screen.findByRole('dialog', { name: 'Статблок NPC: Гоблины' })

    fireEvent.click(await within(dialog).findByRole('button', { name: '🎲 Атаковать' }))
    expect(openRollerMock).toHaveBeenCalledWith(expect.objectContaining({
      kind: 'combat', title: 'Клинок', basePool: { ability: 1, proficiency: 2 }, canSecret: true,
    }))
  })
})
