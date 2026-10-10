import { render, screen, fireEvent, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LibraryPage } from './LibraryPage'

const reference = { archetypes: [], careers: [], skills: [], talents: [], items: [], heroicAbilities: [], qualities: [] }
const createSkill = vi.fn()
const loadReference = vi.fn().mockResolvedValue(reference)
const library = vi.fn()
const membership = vi.fn().mockResolvedValue(undefined)
const history = vi.fn().mockResolvedValue([])
const packs = [{ id: 'pack', name: 'Личный набор', description: '', system: 'genesysCore', entryCount: 0, exclusionCount: 0, isEnabledByDefault: true, updatedAt: '2026-10-09T00:00:00Z' }]
vi.mock('../api/client', () => ({ api: {
  reference: (...args: unknown[]) => loadReference(...args), library: (...args: unknown[]) => library(...args),
  libraryProposals: vi.fn().mockResolvedValue([]),
  createCustomSkill: (...args: unknown[]) => createSkill(...args),
  homebrewPacks: vi.fn(async () => packs), homebrewPackExclusions: vi.fn().mockResolvedValue([]),
  changeHomebrewPackEntries: (...args: unknown[]) => membership(...args),
  homebrewPackChanges: (...args: unknown[]) => history(...args),
} }))
beforeEach(() => { vi.clearAllMocks(); library.mockResolvedValue([]); createSkill.mockResolvedValue({ id: 'skill' }) })

describe('v2 account library', () => {
  it('creates a standalone entry through the full drawer and refreshes metadata', async () => {
    render(<LibraryPage />)
    await screen.findByText('Вне наборов')
    fireEvent.click(screen.getByRole('button', { name: 'Создать контент' }))
    fireEvent.change(await screen.findByLabelText('Название'), { target: { value: 'Sailing' } })
    fireEvent.click(screen.getByRole('button', { name: 'Создать' }))
    await waitFor(() => expect(createSkill).toHaveBeenCalledWith(undefined, {
      system: 'genesysCore', name: 'Sailing', characteristic: 'brawn', kind: 'general', packIds: [],
    }))
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    expect(library.mock.calls.length).toBeGreaterThan(1)
    expect(loadReference).toHaveBeenCalledWith('genesysCore', { library: true })
  })
  it('keeps the drawer and entered values after a failed save', async () => {
    createSkill.mockRejectedValue(new Error('Проверка полей'))
    render(<LibraryPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Создать контент' }))
    fireEvent.change(await screen.findByLabelText('Название'), { target: { value: 'Sailing' } })
    fireEvent.click(screen.getByRole('button', { name: 'Создать' }))
    expect((await screen.findByRole('alert')).textContent).toContain('Проверка полей')
    expect((screen.getByLabelText('Название') as HTMLInputElement).value).toBe('Sailing')
    expect(screen.getByRole('dialog')).toBeTruthy()
  })
  it('shows pack metadata, exclusions and account-only default switch', async () => {
    render(<LibraryPage />)
    fireEvent.click(await screen.findByRole('button', { name: /Личный набор/ }))
    expect(screen.getByRole('heading', { name: 'Личный набор' })).toBeTruthy()
    expect(screen.getByRole('switch', { name: 'Доступен своим персонажам' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Ограничения книг' })).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'История' }))
    await waitFor(() => expect(history).toHaveBeenCalledWith('pack', undefined, 200))
  })
  it('removes membership while preserving the library entry', async () => {
    library.mockResolvedValue([{ id: 'skill', entryType: 'skill', name: 'Sailing', nameRu: '', system: 'genesysCore', meta: '', packIds: ['pack'], lastEditedAt: null }])
    render(<LibraryPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Выбрать наборы' }))
    const dialog = screen.getByRole('dialog')
    fireEvent.click(within(dialog).getByRole('checkbox', { name: 'Личный набор' }))
    await waitFor(() => expect(membership).toHaveBeenCalledWith('pack', [{ entryType: 'skill', entryId: 'skill' }], true))
    expect(screen.getByText('Sailing', { selector: 'strong' })).toBeTruthy()
  })
})
