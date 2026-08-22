import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CampaignDetail, CharacterSheet, GameSession } from '../api/types'
import { CampaignsPage } from './CampaignsPage'

function detail(isGm: boolean, isMine = false): CampaignDetail {
  return {
    id: 'c1', name: 'Поход', description: '', isGm, joinCode: isGm ? 'ABC123' : null,
    members: [{ characterId: 'ch1', characterName: 'Бард', system: 'genesysCore',
      archetype: 'Человек', career: 'Бард', isMine, availableXp: isMine ? 40 : null }],
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
  archetype: { name: 'Человек' },
  career: { name: 'Бард' },
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
      strainThreshold: 11, soak: 3, meleeDefense: 0, rangedDefense: 0, boostDice: 0, setbackDice: 0, criticalInjuries: 0, isActive: true,
      isDefeated: false, isHiddenFromPlayers: false, notes: '', order: 0 },
    { id: 'n1', characterId: null, npcId: 'npc1', displayName: 'Наёмник', participantType: 'npc',
      initiativeSlotType: 'npc', count: 1, woundsCurrent: 5, woundsThreshold: 12, strainCurrent: 0,
      strainThreshold: null, soak: 3, meleeDefense: 0, rangedDefense: 0, boostDice: 0, setbackDice: 0, criticalInjuries: 0, isActive: true,
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
    campaignMemberAudit: vi.fn().mockResolvedValue([]),
    reference: (...a: unknown[]) => referenceMock(...a),
    session: () => sessionMock(),
    removeCampaignCharacter: (...a: unknown[]) => removeCampaignCharacterMock(...a),
    updateSession: (...a: unknown[]) => updateSessionMock(...a),
    nextTurn: (...a: unknown[]) => nextTurnMock(...a),
  },
}))
// Хаб реального времени (SignalR) в jsdom не нужен.
vi.mock('../useCampaignHub', () => ({ useCampaignHub: () => {} }))
vi.mock('../components/SheetTab', () => ({
  SheetTab: ({ sheet, readOnly }: { sheet: { name: string }; readOnly?: boolean }) =>
    <div data-testid="campaign-sheet-page">SHEET:{sheet.name}:{readOnly ? 'READONLY' : 'EDIT'}</div>,
}))
vi.mock('../components/MagicTab', () => ({ MagicTab: () => <div data-testid="readonly-magic">MAGIC</div> }))
vi.mock('../components/HistoryTab', () => ({ HistoryTab: () => <div data-testid="readonly-history">HISTORY</div> }))
vi.mock('../components/CampaignMemberReadOnlyTabs', () => ({
  ReadOnlyTalentsTab: () => <div data-testid="readonly-talents">TALENTS</div>,
  ReadOnlyInventoryTab: () => <div data-testid="readonly-inventory">INVENTORY</div>,
  ReadOnlyHeroicTab: () => <div>HEROIC</div>,
  ReadOnlyAttachmentsTab: () => <div>ATTACHMENTS</div>,
  ReadOnlyTransportTab: () => <div>TRANSPORT</div>,
  ReadOnlyBioTab: () => <div data-testid="readonly-bio">BIO</div>,
}))

const noop = () => {}
const openCharacterMock = vi.fn()
const openOwnCharacterMock = vi.fn()
const props = {
  openId: 'c1', view: 'overview' as const, openEncounterId: null, openCharacterId: null,
  onOpen: noop, onBack: noop, onView: noop, onOpenEncounter: noop, onCloseEncounter: noop,
  onOpenCharacter: openCharacterMock, onCloseCharacter: noop,
  onOpenOwnCharacter: openOwnCharacterMock,
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
    openCharacterMock.mockClear()
    openOwnCharacterMock.mockClear()
  })

  it('GM видит кнопку «Лист» и переходит на страницу участника', async () => {
    campaignMock.mockResolvedValue(detail(true))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getAllByText('Бард').length).toBeGreaterThan(0))
    const sheetBtn = screen.getByRole('button', { name: 'Лист' })
    fireEvent.click(sheetBtn)

    expect(openCharacterMock).toHaveBeenCalledWith('ch1')
    expect(screen.queryByTestId('print-preview')).toBeNull()
  })

  it('deep link открывает экранный read-only лист без печатного оверлея', async () => {
    render(<CampaignsPage {...props} openCharacterId="ch1" />)

    await waitFor(() => expect(memberSheetMock).toHaveBeenCalledWith('c1', 'ch1'))
    expect((await screen.findByTestId('campaign-sheet-page')).textContent).toContain('SHEET:Бард:READONLY')
    expect(screen.getByText('Только просмотр')).toBeTruthy()
    expect(screen.queryByTestId('print-preview')).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Таланты' }))
    expect(screen.getByTestId('readonly-talents')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))
    expect(screen.getByTestId('readonly-inventory')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Образ' }))
    expect(screen.getByTestId('readonly-bio')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'История' }))
    expect(screen.getByTestId('readonly-history')).toBeTruthy()
  })

  it('открывает собственного персонажа на обычной странице со всеми вкладками', async () => {
    campaignMock.mockResolvedValue(detail(false, true))
    render(<CampaignsPage {...props} />)

    const sheetButton = await screen.findByRole('button', { name: 'Лист' })
    fireEvent.click(sheetButton)

    expect(openOwnCharacterMock).toHaveBeenCalledWith('ch1')
    expect(openCharacterMock).not.toHaveBeenCalled()
  })

  it('игрок не видит кнопку «Лист»', async () => {
    campaignMock.mockResolvedValue(detail(false))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getAllByText('Бард').length).toBeGreaterThan(0))
    expect(screen.queryByRole('button', { name: 'Лист' })).toBeNull()
  })

  it('игрок видит доступный XP своего персонажа на обзоре', async () => {
    campaignMock.mockResolvedValue(detail(false, true))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getByText('Персонажи группы')).toBeTruthy())
    expect(screen.getByText('Свободно XP:').parentElement?.textContent).toContain('40')
  })

  it('показывает вкладку кастома только мастеру', async () => {
    const gmView = render(<CampaignsPage {...props} />)
    await screen.findByRole('button', { name: 'Кастом' })
    gmView.unmount()

    campaignMock.mockResolvedValue(detail(false))
    render(<CampaignsPage {...props} />)
    await waitFor(() => expect(screen.getAllByText('Бард').length).toBeGreaterThan(0))
    expect(screen.queryByRole('button', { name: 'Кастом' })).toBeNull()
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
