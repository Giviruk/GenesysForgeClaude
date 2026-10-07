import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CustomTab } from './CustomTab'
import type { Reference } from '../api/types'

const campaignPacks = vi.fn()
const connect = vi.fn()
const toggle = vi.fn()
const importJson = vi.fn()
const ownPacks = vi.fn()
const changes = vi.fn()
const source = { id: 'original-pack', name: 'Исходный набор', system: 'genesysCore', isEnabled: true, isMine: false, entryCount: 1, ownerName: 'Игрок', ownerIsMember: true, lastChangedAt: null, connectedAt: '2026-10-01T12:00:00Z' }
const reference = { skills: [], talents: [], items: [], archetypes: [], careers: [], heroicAbilities: [], qualities: [], heroicSecondaryEffects: [], attachments: [], mounts: [], editableCustomIds: [] } as Reference
vi.mock('../api/client', () => ({ api: {
  homebrewPacks: (...args: unknown[]) => ownPacks(...args),
  homebrewPackChanges: (...args: unknown[]) => changes(...args),
  campaignHomebrewPacks: (...args: unknown[]) => campaignPacks(...args),
  connectSharedCampaignHomebrewPack: (...args: unknown[]) => connect(...args),
  setCampaignHomebrewPack: (...args: unknown[]) => toggle(...args),
  importHomebrewPack: (...args: unknown[]) => importJson(...args),
} }))

beforeEach(() => {
  vi.clearAllMocks()
  ownPacks.mockResolvedValue([])
  changes.mockResolvedValue([])
  campaignPacks.mockResolvedValue([])
  connect.mockImplementation(async () => { campaignPacks.mockResolvedValue([source]); return { id: source.id } })
  toggle.mockImplementation(async (_campaign, _id, isEnabled) => { campaignPacks.mockResolvedValue([{ ...source, isEnabled }]) })
})

describe('исходный набор игрока', () => {
  it('подключает по shared-ссылке и управляет исходным набором без импорта JSON', async () => {
    const refresh = vi.fn().mockResolvedValue(undefined), onError = vi.fn()
    render(<CustomTab campaignId="campaign" system="genesysCore" reference={reference} refresh={refresh} onError={onError} />)
    fireEvent.click(screen.getByRole('button', { name: 'Наборы JSON' }))
    fireEvent.change(await screen.findByLabelText('Shared-ссылка или токен набора игрока'), {
      target: { value: 'https://example.test/homebrew/import/token%20value' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Подключить исходный набор' }))
    await waitFor(() => expect(connect).toHaveBeenCalledWith('campaign', 'token value'))
    await screen.findByText(/Исходный набор/)
    expect(importJson).not.toHaveBeenCalled()
    fireEvent.click(await screen.findByRole('button', { name: 'Отключить набор' }))
    await waitFor(() => expect(toggle).toHaveBeenCalledWith('campaign', 'original-pack', false))
    expect(await screen.findByRole('button', { name: 'Включить набор' })).toBeTruthy()
    expect(onError).not.toHaveBeenCalled()
  })

  it('показывает владельца вышедшего игрока и оставляет ручное отключение', async () => {
    campaignPacks.mockResolvedValue([{ ...source, ownerIsMember: false }])
    render(<CustomTab campaignId="campaign" system="genesysCore" reference={reference} refresh={vi.fn()} onError={vi.fn()} />)
    fireEvent.click(screen.getByRole('button', { name: 'Наборы JSON' }))
    expect(await screen.findByText(/набор игрока Игрок/)).toBeTruthy()
    expect(await screen.findByText(/игрок покинул кампанию/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Отключить набор' })).toBeTruthy()
  })

  it('показывает контент другого автора без кнопок редактирования и удаления', async () => {
    const foreign = { ...reference, skills: [{ id: 'foreign', name: 'Player skill', isCustom: true, characteristic: 'agility', kind: 'general' }] } as Reference
    render(<CustomTab campaignId="campaign" system="genesysCore" reference={foreign} refresh={vi.fn()} onError={vi.fn()} />)
    expect(screen.getByText('Контент автора')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Изменить' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Удалить' })).toBeNull()
  })

  it('показывает дату каждой записи и открывает историю своего набора вне кампании', async () => {
    ownPacks.mockResolvedValue([{ ...source, isMine: true, description: '', isEnabledByDefault: true }])
    const dated: Reference = { ...reference, customLastEditedAt: { personal: '2026-10-07T12:00:00Z' }, editableCustomIds: ['personal'],
      skills: [{ id: 'personal', name: 'Мой навык', nameRu: 'Мой навык', safeDescription: '', source: 'Custom',
        isCustom: true, characteristic: 'agility', kind: 'general' }] }
    render(<CustomTab system="genesysCore" reference={dated} refresh={vi.fn()} onError={vi.fn()} />)
    expect(screen.getByText('изменено 07.10.2026')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Наборы JSON' }))
    fireEvent.click(await screen.findByRole('button', { name: 'История' }))
    await waitFor(() => expect(changes).toHaveBeenCalledWith(source.id, undefined, 200))
  })
})
