import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { LibraryPage } from './LibraryPage'

const reference = { archetypes: [], careers: [], skills: [], talents: [], items: [], heroicAbilities: [], qualities: [] }
const createSkill = vi.fn().mockResolvedValue({ id: 'skill' })
const loadReference = vi.fn().mockResolvedValue(reference)
vi.mock('../api/client', () => ({ api: {
  reference: (...args: unknown[]) => loadReference(...args),
  createCustomSkill: (...args: unknown[]) => createSkill(...args),
  homebrewPacks: vi.fn().mockResolvedValue([{ id: 'pack', name: 'Личный набор', system: 'genesysCore', entryCount: 1 }]),
} }))

describe('personal library', () => {
  it('creates account content without a campaign and refreshes the reference', async () => {
    render(<LibraryPage />)
    fireEvent.change(await screen.findByLabelText('Название'), { target: { value: 'Sailing' } })
    fireEvent.click(screen.getByRole('button', { name: 'Создать навык' }))
    await waitFor(() => expect(createSkill).toHaveBeenCalledWith(undefined, {
      system: 'genesysCore', name: 'Sailing', characteristic: 'brawn', kind: 'general',
    }))
    expect(await screen.findByText(/Навык «Sailing» создан/)).toBeTruthy()
    expect(loadReference.mock.calls.length).toBeGreaterThan(1)
  })

  it('lists personal packs without campaign toggle actions', async () => {
    render(<LibraryPage />)
    await screen.findByLabelText('Название')
    fireEvent.click(screen.getByRole('button', { name: 'Наборы JSON' }))
    expect(await screen.findByText(/Личный набор/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Для кампании: вкл.' })).toBeNull()
  })
})
