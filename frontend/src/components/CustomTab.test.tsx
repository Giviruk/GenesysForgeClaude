import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CampaignContentTab } from './content/CampaignContentTab'
import { CustomTab } from './CustomTab'
import type { Reference } from '../api/types'

const campaignPacks = vi.fn()
const connect = vi.fn()
const toggle = vi.fn()
const importJson = vi.fn()
const ownPacks = vi.fn()
const changes = vi.fn()
const source = { id: 'original-pack', name: 'Исходный набор', system: 'genesysCore', isEnabled: true, isMine: false, entryCount: 1, ownerName: 'Игрок', ownerIsMember: true, lastChangedAt: null, connectedAt: '2026-10-01T12:00:00Z', changedAfterConnection: false, updatePolicy: 'auto', status: 'active', exclusionCount: 0, entries: [] }
const reference = { skills: [], talents: [], items: [], archetypes: [], careers: [], heroicAbilities: [], qualities: [], heroicSecondaryEffects: [], attachments: [], mounts: [], editableCustomIds: [] } as Reference
vi.mock('../api/client', () => ({ api: {
  homebrewPacks: (...args: unknown[]) => ownPacks(...args),
  homebrewPackChanges: (...args: unknown[]) => changes(...args),
  campaignContent: async () => ({ systems: [{ system: 'genesysCore', isOpen: true, characters: [], categories: [] }], packs: await campaignPacks(), items: [], alerts: [], availableCount: 1, overrideCount: 0 }),
  campaignHomebrewPacks: (...args: unknown[]) => campaignPacks(...args),
  connectSharedCampaignHomebrewPack: (...args: unknown[]) => connect(...args),
  setCampaignHomebrewPack: (...args: unknown[]) => toggle(...args),
  importHomebrewPack: (...args: unknown[]) => importJson(...args),
} }))

beforeEach(() => {
  vi.clearAllMocks()
  window.history.replaceState(null, '', '/campaigns/campaign/content?section=packs')
  ownPacks.mockResolvedValue([])
  changes.mockResolvedValue([])
  campaignPacks.mockResolvedValue([])
  connect.mockImplementation(async () => { campaignPacks.mockResolvedValue([source]); return { id: source.id } })
  toggle.mockImplementation(async (_campaign, _id, isEnabled) => { campaignPacks.mockResolvedValue([{ ...source, isEnabled }]) })
})

describe('исходный набор игрока', () => {
  it('подключает по shared-ссылке и управляет исходным набором без импорта JSON', async () => {
    render(<CampaignContentTab campaignId="campaign" />)
    fireEvent.click(await screen.findByRole('button', { name: 'По ссылке игрока' }))
    fireEvent.change(await screen.findByLabelText('Ссылка или токен'), {
      target: { value: 'https://example.test/homebrew/import/token%20value' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Подключить' }))
    await waitFor(() => expect(connect).toHaveBeenCalledWith('campaign', 'token value'))
    await screen.findByRole('heading', { name: 'Исходный набор' })
    expect(importJson).not.toHaveBeenCalled()
    fireEvent.click(await screen.findByRole('switch', { name: 'Подключён: Исходный набор' }))
    await waitFor(() => expect(toggle).toHaveBeenCalledWith('campaign', 'original-pack', false))
    await waitFor(() => expect(screen.getByRole('switch', { name: 'Подключён: Исходный набор' }).getAttribute('aria-checked')).toBe('false'))
  })

  it('показывает владельца вышедшего игрока и оставляет ручное отключение', async () => {
    campaignPacks.mockResolvedValue([{ ...source, ownerIsMember: false }])
    render(<CampaignContentTab campaignId="campaign" />)
    expect(await screen.findByText(/Игрок/)).toBeTruthy()
    expect(await screen.findByText(/автор покинул кампанию/)).toBeTruthy()
    expect(screen.getByRole('switch', { name: 'Подключён: Исходный набор' })).toBeTruthy()
  })

  it('показывает контент другого автора без кнопок редактирования и удаления', async () => {
    const foreign = { ...reference, skills: [{ id: 'foreign', name: 'Player skill', isCustom: true, characteristic: 'agility', kind: 'general' }] } as Reference
    render(<CustomTab campaignId="campaign" system="genesysCore" reference={foreign} refresh={vi.fn()} onError={vi.fn()} />)
    expect(screen.getByText('Контент автора')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Изменить' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Удалить' })).toBeNull()
    expect(screen.queryByText('Дата правки неизвестна')).toBeNull()
    expect(screen.getByText(/Player skill/).textContent).not.toMatch(/ · $/)
    expect(screen.getByText(/Player skill/).querySelector('.small-text')).toBeNull()
  })

  it('показывает дату каждой записи в старом редакторе листа', async () => {
    ownPacks.mockResolvedValue([{ ...source, isMine: true, description: '', isEnabledByDefault: true }])
    const dated: Reference = { ...reference, customLastEditedAt: { personal: '2026-10-07T12:00:00Z' }, editableCustomIds: ['personal'],
      skills: [{ id: 'personal', name: 'Мой навык', nameRu: 'Мой навык', safeDescription: '', source: 'Custom',
        isCustom: true, characteristic: 'agility', kind: 'general' }] }
    render(<CustomTab system="genesysCore" reference={dated} refresh={vi.fn()} onError={vi.fn()} />)
    expect(screen.getByText('изменено 07.10.2026')).toBeTruthy()

  })
})
