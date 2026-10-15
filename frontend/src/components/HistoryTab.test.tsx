import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import type { CharacterAuditEntry, Reference } from '../api/types'
import { HistoryTab } from './HistoryTab'

afterEach(() => vi.restoreAllMocks())

describe('HistoryTab', () => {
  it('labels mount, repair, cargo and unknown actions accurately without rebuilding the name index while typing', async () => {
    const getName = vi.fn(() => 'Beast of Burden')
    const reference = { mounts: [{ get name() { return getName() }, nameRu: 'Вьючный зверь' }] } as unknown as Reference
    const actions = ['mountBought', 'itemRepaired', 'cargoMoved', 'futureAction'] as CharacterAuditEntry['action'][]
    const entries = actions.map((action, i) => ({ id: String(i), action, createdAt: '2026-10-15T00:00:00Z',
      summary: i === 0 ? 'Приобретён скакун «Beast of Burden»' : `Операция ${i}`,
      xpDelta: null, totalXpAfter: 100, spentXpAfter: 50, canUndo: false }))
    render(<HistoryTab characterId="ch1" reference={reference} onError={vi.fn()} refresh={vi.fn()}
      loadEntries={vi.fn().mockResolvedValue(entries)} />)
    await screen.findByText('Приобретён скакун «Вьючный зверь»')
    const rows = screen.getAllByRole('row').slice(1)
    for (const [i, label] of ['Скакун +', 'Ремонт', 'Перенос груза', 'Действие'].entries()) {
      expect(within(rows[i]).getByText(label)).toBeTruthy()
    }
    const reads = getName.mock.calls.length
    fireEvent.change(screen.getByPlaceholderText('±XP'), { target: { value: '5' } })
    fireEvent.change(screen.getByPlaceholderText('Комментарий (необязательно)'), { target: { value: 'Награда' } })
    expect(getName).toHaveBeenCalledTimes(reads)
  })
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
