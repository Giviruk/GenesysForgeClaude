import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CampaignMember, GameParticipant, GameSession, NpcDetail, Reference } from '../api/types'
import type { DiceRollerRequest } from '../dice-roller-store'
import { GameTableTab } from './GameTableTab'

const sessionMock = vi.fn()
const npcMock = vi.fn()
const referenceMock = vi.fn()
const rollsMock = vi.fn()
const createRollMock = vi.fn()
const updateParticipantMock = vi.fn()
const openRollerMock = vi.fn()
const npcsMock = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    session: (...args: unknown[]) => sessionMock(...args),
    npc: (...args: unknown[]) => npcMock(...args),
    reference: (...args: unknown[]) => referenceMock(...args),
    rolls: (...args: unknown[]) => rollsMock(...args),
    createRoll: (...args: unknown[]) => createRollMock(...args),
    updateParticipant: (...args: unknown[]) => updateParticipantMock(...args),
    npcs: (...args: unknown[]) => npcsMock(...args),
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
    woundsCurrent: 5, woundsThreshold: 15, strainCurrent: 0, strainThreshold: null,
    soak: 3, meleeDefense: 0, rangedDefense: 0, boostDice: 2, setbackDice: 1, criticalInjuries: 0,
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

const playerParticipant: GameParticipant = {
  ...session.participants[0], id: 'participant-pc', characterId: 'character-1', npcId: null,
  displayName: 'Элира', participantType: 'playerCharacter', initiativeSlotType: 'player', count: 1,
  woundsCurrent: 1, woundsThreshold: 12, strainCurrent: 2, strainThreshold: 11,
  soak: 2, meleeDefense: 1, rangedDefense: 0, boostDice: 0, setbackDice: 0,
}

const ownMember: CampaignMember = {
  characterId: 'character-1', characterName: 'Элира', system: 'realmsOfTerrinoth',
  archetype: 'Human', career: 'Warrior', isMine: true,
}

describe('GameTableTab — статблок и броски NPC', () => {
  beforeEach(() => {
    localStorage.removeItem('genesysforge.game-table.range.campaign-1.session-1')
    sessionMock.mockReset().mockResolvedValue(session)
    npcMock.mockReset().mockResolvedValue(npc)
    referenceMock.mockReset().mockResolvedValue(reference)
    rollsMock.mockReset().mockResolvedValue([])
    createRollMock.mockReset().mockResolvedValue({})
    updateParticipantMock.mockReset().mockResolvedValue(session)
    openRollerMock.mockReset()
    npcsMock.mockReset().mockResolvedValue([])
    window.history.replaceState(null, '', '/')
    localStorage.removeItem('genesysforge.sheet-tab.character-1')
  })

  it('сохраняет позиции трекера дистанций при повторном открытии игрового стола', async () => {
    const rangeSession = { ...session, participants: [playerParticipant, session.participants[0]] }
    sessionMock.mockResolvedValue(rangeSession)
    const first = render(<GameTableTab campaignId="campaign-1" isGm members={[ownMember]} />)
    await screen.findByText('Засада')
    const token = document.querySelector('.ring-token.npc') as HTMLElement
    expect(token.title).toContain('Средняя')

    fireEvent.click(within(token).getByTitle('Дальше'))
    expect((document.querySelector('.ring-token.npc') as HTMLElement).title).toContain('Дальняя')
    first.unmount()

    render(<GameTableTab campaignId="campaign-1" isGm members={[ownMember]} />)
    await screen.findByText('Засада')
    const restored = document.querySelector('.ring-token.npc') as HTMLElement
    expect(restored.title).toContain('Дальняя')
    expect(screen.getByText(/Средняя → Дальняя/)).toBeTruthy()
  })

  it('показывает расстояния от выбранного участника, не переставляя токены', async () => {
    const rangeSession = { ...session, participants: [playerParticipant, session.participants[0]] }
    sessionMock.mockResolvedValue(rangeSession)
    render(<GameTableTab campaignId="campaign-1" isGm members={[ownMember]} />)

    const npcToken = await screen.findByRole('button', { name: 'Выбрать Гоблины ×3/3 для расчёта расстояний' })
    const pcToken = screen.getByRole('button', { name: 'Выбрать Элира для расчёта расстояний' })
    const npcPosition = npcToken.getAttribute('style')
    const pcPosition = pcToken.getAttribute('style')

    expect(screen.getByText('Расчётные расстояния от Элира')).toBeTruthy()
    expect(screen.getByText('≈ Средняя')).toBeTruthy()
    fireEvent.click(npcToken)

    expect(screen.getByText('Расчётные расстояния от Гоблины ×3/3')).toBeTruthy()
    expect(npcToken.getAttribute('style')).toBe(npcPosition)
    expect(pcToken.getAttribute('style')).toBe(pcPosition)
  })

  it('перетаскивает токен указателем в центр выбранной ячейки', async () => {
    const rangeSession = { ...session, participants: [playerParticipant, session.participants[0]] }
    sessionMock.mockResolvedValue(rangeSession)
    render(<GameTableTab campaignId="campaign-1" isGm members={[ownMember]} />)

    const npcToken = await screen.findByRole('button', { name: 'Выбрать Гоблины ×3/3 для расчёта расстояний' })
    const board = screen.getByTestId('range-board')
    vi.spyOn(board, 'getBoundingClientRect').mockReturnValue({
      left: 100, top: 50, width: 400, height: 400, right: 500, bottom: 450,
      x: 100, y: 50, toJSON: () => ({}),
    })

    fireEvent.pointerDown(npcToken, { button: 0, pointerId: 1, clientX: 300, clientY: 250 })
    fireEvent.pointerMove(board, { buttons: 1, pointerId: 1, clientX: 330, clientY: 250 })

    const preview = document.querySelector('.ring-token.drag-preview') as HTMLElement
    expect(Number.parseFloat(preview.style.left)).toBeCloseTo(57.5, 4)
    expect(Number.parseFloat(preview.style.top)).toBeCloseTo(50, 4)
    expect(Number.parseFloat((document.querySelector('.drag-cell-target') as HTMLElement).style.left)).toBeCloseTo(64.2, 2)

    fireEvent.pointerUp(board, { button: 0, pointerId: 1, clientX: 330, clientY: 250 })

    expect(npcToken.title).toContain('Ближняя, справа')
    expect(Number.parseFloat(npcToken.style.left)).toBeCloseTo(64.20, 2)
    expect(Number.parseFloat(npcToken.style.top)).toBeCloseTo(53.805, 2)
  })

  it('считает оставшихся миньонов по ранам и использует их число в навыках', async () => {
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Открыть статблок NPC Гоблины' }))
    const dialog = await screen.findByRole('dialog', { name: 'Статблок NPC: Гоблины' })

    expect(npcMock).toHaveBeenCalledWith('npc-1')
    expect(await within(dialog).findByText('Осталось 3 из 3 · индивидуальный порог ран 5 · эффективный ранг 2')).toBeTruthy()
    expect(within(dialog).getByText('Небольшой опасный противник.')).toBeTruthy()
    expect(within(dialog).getByText('Держатся группой.')).toBeTruthy()

    fireEvent.click(within(dialog).getByRole('button', { name: '🎲 Бросить' }))
    const request = openRollerMock.mock.calls[0][0] as Extract<DiceRollerRequest, { kind: 'roll' }>
    expect(request.initialPool).toEqual({ ability: 1, proficiency: 2, boost: 2, setback: 1 })
    expect(request.label).toBe('Melee')

    request.onLog?.({ poolJson: '{}', resultJson: '{}', summary: 'ok', label: 'Melee', isSecret: false })
    await waitFor(() => expect(createRollMock).toHaveBeenCalledWith('campaign-1', expect.objectContaining({
      actorName: 'Гоблины', label: 'Melee',
    })))
  })

  it('после раны, пересекающей порог миньона, обновляет остаток и пул без закрытия карточки', async () => {
    const wounded = {
      ...session,
      participants: [{ ...session.participants[0], woundsCurrent: 6 }],
    }
    updateParticipantMock.mockResolvedValue(wounded)
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)
    fireEvent.click(await screen.findByRole('button', { name: 'Открыть статблок NPC Гоблины' }))
    const dialog = await screen.findByRole('dialog', { name: 'Статблок NPC: Гоблины' })

    fireEvent.click(within(dialog).getByLabelText('Управление состоянием NPC').querySelectorAll('button')[1])

    await waitFor(() => expect(updateParticipantMock).toHaveBeenCalledWith(
      'campaign-1', 'participant-1', { woundsCurrent: 6 },
    ))
    expect(await within(dialog).findByText('Осталось 2 из 3 · индивидуальный порог ран 5 · эффективный ранг 1')).toBeTruthy()
    expect(screen.getAllByText('Гоблины ×2/3').length).toBeGreaterThanOrEqual(2)

    openRollerMock.mockReset()
    fireEvent.click(within(dialog).getByRole('button', { name: '🎲 Бросить' }))
    expect(openRollerMock).toHaveBeenCalledWith(expect.objectContaining({
      kind: 'roll', initialPool: { ability: 2, proficiency: 1, boost: 2, setback: 1 },
    }))
  })

  it('позволяет мастеру менять усталость одиночного NPC из статблока', async () => {
    const rivalSession = {
      ...session,
      participants: [{ ...session.participants[0], participantType: 'npc' as const, count: 1,
        woundsCurrent: 0, woundsThreshold: 10, strainCurrent: 2, strainThreshold: 8 }],
    }
    const updated = {
      ...rivalSession,
      participants: [{ ...rivalSession.participants[0], strainCurrent: 3 }],
    }
    sessionMock.mockResolvedValue(rivalSession)
    npcMock.mockResolvedValue({ ...npc, kind: 'rival', woundThreshold: 10, strainThreshold: 8 })
    updateParticipantMock.mockResolvedValue(updated)
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)
    fireEvent.click(await screen.findByRole('button', { name: 'Открыть статблок NPC Гоблины' }))
    const controls = within(await screen.findByRole('dialog')).getByLabelText('Управление состоянием NPC')

    fireEvent.click(within(controls).getAllByRole('button', { name: '+1' })[1])
    await waitFor(() => expect(updateParticipantMock).toHaveBeenCalledWith(
      'campaign-1', 'participant-1', { strainCurrent: 3 },
    ))
  })

  it('открывает боевой дайсроллер атаки с тем же групповым пулом', async () => {
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)
    fireEvent.click(await screen.findByRole('button', { name: 'Открыть статблок NPC Гоблины' }))
    const dialog = await screen.findByRole('dialog', { name: 'Статблок NPC: Гоблины' })

    fireEvent.click(await within(dialog).findByRole('button', { name: '🎲 Атаковать' }))
    expect(openRollerMock).toHaveBeenCalledWith(expect.objectContaining({
      kind: 'combat', title: 'Клинок', basePool: { ability: 1, proficiency: 2, boost: 2, setback: 1 }, canSecret: true,
    }))
  })

  it('назначает участнику бусты и сетбеки и открывает общий бросок с ними', async () => {
    const updated = {
      ...session,
      participants: [{ ...session.participants[0], boostDice: 3, setbackDice: 1 }],
    }
    updateParticipantMock.mockResolvedValue(updated)
    render(<GameTableTab campaignId="campaign-1" isGm members={[]} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Добавить буст Гоблины' }))
    await waitFor(() => expect(updateParticipantMock).toHaveBeenCalledWith(
      'campaign-1', 'participant-1', { boostDice: 3 },
    ))

    fireEvent.click(screen.getByRole('button', { name: 'Бросок участника Гоблины' }))
    expect(openRollerMock).toHaveBeenCalledWith(expect.objectContaining({
      kind: 'roll', initialPool: { boost: 3, setback: 1 },
    }))
  })

  it('открывает собственный лист игрока на вкладке «Лист»', async () => {
    sessionMock.mockResolvedValue({ ...session, participants: [playerParticipant] })
    localStorage.setItem('genesysforge.sheet-tab.character-1', 'inventory')
    render(<GameTableTab campaignId="campaign-1" isGm={false} members={[ownMember]} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Открыть лист персонажа Элира' }))

    expect(window.location.pathname).toBe('/characters/character-1')
    expect(localStorage.getItem('genesysforge.sheet-tab.character-1')).toBe('sheet')
  })

  it('не открывает чужой лист игроку, но передаёт его мастеру для read-only просмотра', async () => {
    const otherMember = { ...ownMember, isMine: false }
    const pcSession = { ...session, participants: [playerParticipant] }
    sessionMock.mockResolvedValue(pcSession)
    const playerView = render(<GameTableTab campaignId="campaign-1" isGm={false} members={[otherMember]} />)
    expect((await screen.findAllByText('Элира')).length).toBeGreaterThan(0)
    expect(screen.queryByRole('button', { name: 'Открыть лист персонажа Элира' })).toBeNull()
    playerView.unmount()

    const openMemberSheet = vi.fn().mockResolvedValue(undefined)
    render(<GameTableTab campaignId="campaign-1" isGm members={[otherMember]}
      onOpenMemberSheet={openMemberSheet} />)
    fireEvent.click(await screen.findByRole('button', { name: 'Открыть лист персонажа Элира' }))
    expect(openMemberSheet).toHaveBeenCalledWith('character-1', 'Элира')
  })
})
