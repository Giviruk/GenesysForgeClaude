import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CampaignDetail, CharacterSheet, GameSession } from '../api/types'
import { CampaignsPage } from './CampaignsPage'

function detail(isGm: boolean): CampaignDetail {
  return {
    id: 'c1', name: 'Поход', description: '', isGm, joinCode: isGm ? 'ABC123' : null,
    members: [{ characterId: 'ch1', characterName: 'Бард', system: 'genesysCore',
      archetype: 'Человек', career: 'Бард', isMine: false }],
    notes: [],
  }
}

const campaignMock = vi.fn()
const sessionMock = vi.fn()
const removeCampaignCharacterMock = vi.fn().mockResolvedValue(undefined)
const updateSessionMock = vi.fn()
const nextTurnMock = vi.fn()
const sheet = {
  id: 'ch1',
  name: 'Бард',
  system: 'genesysCore',
  totalXp: 120,
  spentXp: 80,
  availableXp: 40,
  woundsCurrent: 4,
  strainCurrent: 3,
  derived: { woundThreshold: 12, strainThreshold: 11 },
} as unknown as CharacterSheet
const memberSheetMock = vi.fn().mockResolvedValue(sheet)
const referenceMock = vi.fn().mockResolvedValue({})
const session = {
  id: 's1',
  campaignId: 'c1',
  name: 'Засада в доках',
  description: '',
  isActive: true,
  isGm: true,
  allowPlayerEdits: false,
  playerStoryPoints: 2,
  gmStoryPoints: 3,
  currentRound: 2,
  currentTurnIndex: 0,
  publicNotes: '',
  gmNotes: null,
  participants: [
    { id: 'p1', characterId: 'ch1', npcId: null, displayName: 'Бард', participantType: 'playerCharacter',
      initiativeSlotType: 'player', count: 1, woundsCurrent: 0, woundsThreshold: 12, strainCurrent: 0,
      strainThreshold: 11, soak: 3, meleeDefense: 0, rangedDefense: 0, criticalInjuries: 0, isActive: true,
      isDefeated: false, isHiddenFromPlayers: false, notes: '', order: 0 },
    { id: 'n1', characterId: null, npcId: 'npc1', displayName: 'Наёмник', participantType: 'npc',
      initiativeSlotType: 'npc', count: 1, woundsCurrent: 5, woundsThreshold: 12, strainCurrent: 0,
      strainThreshold: null, soak: 3, meleeDefense: 0, rangedDefense: 0, criticalInjuries: 0, isActive: true,
      isDefeated: false, isHiddenFromPlayers: false, notes: '', order: 1 },
  ],
  slots: [
    { id: 'slot1', slotType: 'player', order: 0, assignedParticipantId: 'p1', notes: '' },
    { id: 'slot2', slotType: 'npc', order: 1, assignedParticipantId: 'n1', notes: '' },
  ],
} as GameSession
vi.mock('../api/client', () => ({
  api: {
    campaign: () => campaignMock(),
    campaignMemberSheet: (...a: unknown[]) => memberSheetMock(...a),
    reference: (...a: unknown[]) => referenceMock(...a),
    session: () => sessionMock(),
    removeCampaignCharacter: (...a: unknown[]) => removeCampaignCharacterMock(...a),
    updateSession: (...a: unknown[]) => updateSessionMock(...a),
    nextTurn: (...a: unknown[]) => nextTurnMock(...a),
  },
}))
// Хаб реального времени (SignalR) в jsdom не нужен.
vi.mock('../useCampaignHub', () => ({ useCampaignHub: () => {} }))
// Печатный лист и оверлей мокаем — проверяем факт рендера, а не полный статблок.
vi.mock('../components/print/PrintPreview', () => ({
  PrintPreview: ({ title, children }: { title: string; children: (v: string) => React.ReactNode }) =>
    <div data-testid="print-preview">{title}{children('gm')}</div>,
}))
vi.mock('../components/print/CharacterSheetPrint', () => ({
  CharacterSheetPrint: ({ sheet }: { sheet: { name: string } }) => <div>SHEET:{sheet.name}</div>,
}))

const noop = () => {}
const props = {
  openId: 'c1', view: 'overview' as const, openEncounterId: null,
  onOpen: noop, onBack: noop, onView: noop, onOpenEncounter: noop, onCloseEncounter: noop,
}

describe('CampaignsPage — GM просмотр листа участника (U-20)', () => {
  beforeEach(() => {
    campaignMock.mockReset()
    campaignMock.mockResolvedValue(detail(true))
    sessionMock.mockReset()
    sessionMock.mockResolvedValue(session)
    memberSheetMock.mockClear()
    removeCampaignCharacterMock.mockClear()
    updateSessionMock.mockReset()
    updateSessionMock.mockResolvedValue(session)
    nextTurnMock.mockReset()
    nextTurnMock.mockResolvedValue(session)
  })

  it('GM видит кнопку «Лист» и открывает read-only лист участника', async () => {
    campaignMock.mockResolvedValue(detail(true))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getAllByText('Бард').length).toBeGreaterThan(0))
    const sheetBtn = screen.getByRole('button', { name: 'Лист' })
    fireEvent.click(sheetBtn)

    await waitFor(() => expect(memberSheetMock).toHaveBeenCalledWith('c1', 'ch1'))
    await waitFor(() => expect(screen.getByTestId('print-preview')).toBeTruthy())
    expect(screen.getByText('SHEET:Бард')).toBeTruthy()
  })

  it('игрок не видит кнопку «Лист»', async () => {
    campaignMock.mockResolvedValue(detail(false))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getAllByText('Бард').length).toBeGreaterThan(0))
    expect(screen.queryByRole('button', { name: 'Лист' })).toBeNull()
  })

  it('показывает dashboard overview с активной сценой, сюжетными очками и статистикой', async () => {
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getByText('Персонажи группы')).toBeTruthy())
    await waitFor(() => expect(screen.getByText(/Засада в доках/)).toBeTruthy())
    expect(screen.getAllByText(/Раунд 2/).length).toBeGreaterThan(0)
    expect(screen.getAllByText('Наёмник').length).toBeGreaterThan(0)
    expect(screen.getByText('суммарный XP')).toBeTruthy()
    expect(screen.getByText('свободный XP')).toBeTruthy()
    await waitFor(() => expect(screen.getByText('120')).toBeTruthy())
  })
})
