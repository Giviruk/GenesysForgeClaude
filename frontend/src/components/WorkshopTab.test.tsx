import { render, screen, fireEvent } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterSheet, Reference } from '../api/types'
import { WorkshopTab } from './WorkshopTab'

vi.mock('./AttachmentsTab', () => ({ AttachmentsTab: () => <div>Attachments content</div> }))
vi.mock('./CraftingTab', () => ({ CraftingTab: () => <div>Crafting content</div> }))
const props = { sheet: { id: 'workshop-character' } as CharacterSheet,
  reference: {} as Reference, refresh: async () => {}, onError: () => {} }

describe('WorkshopTab', () => {
  beforeEach(() => localStorage.removeItem('genesysforge.workshop-mode.workshop-character'))
  it('remembers the chosen mode after reopening', () => {
    const view = render(<WorkshopTab {...props} />)
    expect(screen.getByText('Attachments content')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Ремесло' }))
    expect(screen.getByText('Crafting content')).toBeTruthy()
    view.unmount()
    render(<WorkshopTab {...props} />)
    expect(screen.getByRole('button', { name: 'Ремесло' }).getAttribute('aria-pressed')).toBe('true')
    expect(screen.getByText('Crafting content')).toBeTruthy()
  })
})
