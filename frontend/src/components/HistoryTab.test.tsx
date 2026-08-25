import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import type { CharacterAuditEntry } from '../api/types'
import { HistoryTab } from './HistoryTab'

afterEach(() => vi.restoreAllMocks())

describe('HistoryTab', () => {
  it('loads campaign-provided history and hides XP controls', async () => {
    const entry: CharacterAuditEntry = {
      id: 'a1', createdAt: '2026-08-15T00:00:00Z', action: 'manualEdit', summary: 'Имя изменено',
      xpDelta: null, totalXpAfter: 100, spentXpAfter: 50, canUndo: false,
    }
    const loadEntries = vi.fn().mockResolvedValue([entry])

    render(<HistoryTab characterId="ch1" onError={vi.fn()} refresh={vi.fn()}
      readOnly loadEntries={loadEntries} />)

    await waitFor(() => expect(loadEntries).toHaveBeenCalledOnce())
    expect(await screen.findByText('Имя изменено')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'Выдать XP' })).toBeNull()
  })

  it('отменяет доступную покупку и обновляет историю и лист', async () => {
    const entry: CharacterAuditEntry = {
      id: 'a2', createdAt: '2026-08-15T00:00:00Z', action: 'skillRankBought', summary: 'Куплен ранг навыка',
      xpDelta: -5, totalXpAfter: 100, spentXpAfter: 55, canUndo: true,
    }
    const loadEntries = vi.fn().mockResolvedValue([entry])
    const undoCharacterAudit = vi.spyOn(api, 'undoCharacterAudit').mockResolvedValue(undefined)
    const refresh = vi.fn().mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    render(<HistoryTab characterId="ch1" onError={vi.fn()} refresh={refresh} loadEntries={loadEntries} />)

    await waitFor(() => expect(loadEntries).toHaveBeenCalledOnce())
    await screen.findByText('Куплен ранг навыка')
    await screen.getByRole('button', { name: 'Отменить' }).click()

    await waitFor(() => expect(undoCharacterAudit).toHaveBeenCalledWith('ch1', 'a2'))
    expect(refresh).toHaveBeenCalledOnce()
  })
})
