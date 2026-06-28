import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CampaignDetail } from '../api/types'
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
const memberSheetMock = vi.fn().mockResolvedValue({ id: 'ch1', name: 'Бард', system: 'genesysCore' })
const referenceMock = vi.fn().mockResolvedValue({})
vi.mock('../api/client', () => ({
  api: {
    campaign: () => campaignMock(),
    campaignMemberSheet: (...a: unknown[]) => memberSheetMock(...a),
    reference: (...a: unknown[]) => referenceMock(...a),
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
  beforeEach(() => { memberSheetMock.mockClear() })

  it('GM видит кнопку «Лист» и открывает read-only лист участника', async () => {
    campaignMock.mockResolvedValue(detail(true))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getByText('Бард')).toBeTruthy())
    const sheetBtn = screen.getByRole('button', { name: 'Лист' })
    fireEvent.click(sheetBtn)

    await waitFor(() => expect(memberSheetMock).toHaveBeenCalledWith('c1', 'ch1'))
    await waitFor(() => expect(screen.getByTestId('print-preview')).toBeTruthy())
    expect(screen.getByText('SHEET:Бард')).toBeTruthy()
  })

  it('игрок не видит кнопку «Лист»', async () => {
    campaignMock.mockResolvedValue(detail(false))
    render(<CampaignsPage {...props} />)

    await waitFor(() => expect(screen.getByText('Бард')).toBeTruthy())
    expect(screen.queryByRole('button', { name: 'Лист' })).toBeNull()
  })
})
