import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { CharacterAuditEntry } from '../api/types'
import { HistoryTab } from './HistoryTab'

describe('HistoryTab — read only', () => {
  it('loads campaign-provided history and hides XP controls', async () => {
    const entry: CharacterAuditEntry = {
      id: 'a1', createdAt: '2026-08-15T00:00:00Z', action: 'manualEdit', summary: 'Имя изменено',
      xpDelta: null, totalXpAfter: 100, spentXpAfter: 50,
    }
    const loadEntries = vi.fn().mockResolvedValue([entry])

    render(<HistoryTab characterId="ch1" onError={vi.fn()} refresh={vi.fn()}
      readOnly loadEntries={loadEntries} />)

    await waitFor(() => expect(loadEntries).toHaveBeenCalledOnce())
    expect(await screen.findByText('Имя изменено')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'Выдать XP' })).toBeNull()
  })
})
